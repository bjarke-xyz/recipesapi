using FirebaseAdmin.Auth;
using RecipesAPI.API.Admin.Common;
using RecipesAPI.API.Exceptions;
using RecipesAPI.API.Infrastructure;
using RecipesAPI.API.Users.Common;
using RecipesAPI.API.Users.DAL;

namespace RecipesAPI.API.Users.BLL;

public class UserService : ICacheKeyGetter
{
    private readonly ILogger<UserService> logger;
    private readonly DAL.UserRepository userRepository;
    private readonly ICacheProvider cache;
    private readonly IEmailService emailService;

    public const string GetUsersCacheKey = "GetUsers";
    public string UserByIdCacheKey(string userId) => $"GetUserById:{userId}";
    public string UserInfoByIdCacheKey(string userId) => $"GetUserInfo:{userId}";
    public const string UserCountCacheKey = "UserCount";

    public UserService(DAL.UserRepository userRepository, ICacheProvider cache, ILogger<UserService> logger, IEmailService emailService)
    {
        this.userRepository = userRepository;
        this.cache = cache;
        this.logger = logger;
        this.emailService = emailService;
    }

    public CacheKeyInfo GetCacheKeyInfo()
    {
        return new CacheKeyInfo
        {
            CacheKeyPrefixes = new List<string>{
                GetUsersCacheKey,
                UserCountCacheKey,
                UserByIdCacheKey(""),
                UserInfoByIdCacheKey(""),
            },
            ResourceType = CachedResourceTypeHelper.USERS,
        };
    }

    public async Task SendResetPasswordMail(string email, CancellationToken cancellationToken)
    {
        try
        {
            var passwordResetLink = await FirebaseAuth.DefaultInstance.GeneratePasswordResetLinkAsync(email, null, cancellationToken);
            ArgumentNullException.ThrowIfNull(passwordResetLink);
            await emailService.SendEmail(email, "Reset password", $"Nogen har bedt om at resette dit password. Tryk på linket for at fortsætte: {passwordResetLink}");
        }
        catch (FirebaseAuthException ex)
        {
            logger.LogError(ex, "Failed to send reset password to {email}", email);
        }
    }

    private void EnrichUsers(List<User> users, Dictionary<string, UserInfo> userInfos)
    {
        foreach (var user in users)
        {
            if (userInfos.TryGetValue(user.Id, out var userInfo) && userInfo != null)
            {
                user.DisplayName = user.DisplayName ?? userInfo.Name;
                user.Role = userInfo.Role;
            }
        }
    }

    public async Task<int> GetUserCount(CancellationToken cancellationToken)
    {
        var userCount = await cache.Get<UserCount>(UserCountCacheKey, cancellationToken);
        if (userCount == null)
        {
            var userCountInt = await userRepository.GetUserCount(cancellationToken);
            userCount = new UserCount { Count = userCountInt };
            await cache.Put(UserCountCacheKey, userCount, TimeSpan.FromDays(7), cancellationToken);
        }
        return userCount.Count;
    }

    public async Task<List<User>> GetUsers(CancellationToken cancellationToken)
    {
        var cached = await cache.Get<List<User>>(GetUsersCacheKey);
        if (cached == null)
        {
            cached = await userRepository.GetUsers(cancellationToken);
            await cache.Put(GetUsersCacheKey, cached);
        }
        var userInfos = await GetUserInfos(cached.Select(x => x.Id).ToList(), cancellationToken);
        EnrichUsers(cached, userInfos);
        return cached;
    }

    public async Task<Dictionary<string, User>> GetUsersByIds(IReadOnlyList<string> userIds, CancellationToken cancellationToken)
    {
        var mutableUserIds = userIds.ToList();
        var fromCache = await cache.Get<User>(userIds.Select(x => UserByIdCacheKey(x)).ToList(), cancellationToken);
        var users = new List<User>();
        foreach (var user in fromCache)
        {
            if (user != null)
            {
                users.Add(user);
                mutableUserIds.Remove(user.Id);
            }
        }

        if (mutableUserIds.Count > 0)
        {
            var allUsers = await userRepository.GetUsers(cancellationToken);
            var usersById = allUsers.GroupBy(x => x.Id).ToDictionary(x => x.Key, x => x.First());
            foreach (var userId in mutableUserIds)
            {
                if (usersById.TryGetValue(userId, out var user))
                {
                    await cache.Put(UserByIdCacheKey(user.Id), user);
                    users.Add(user);
                }
            }
        }
        var userInfos = await GetUserInfos(users.Select(x => x.Id).ToList(), cancellationToken);
        EnrichUsers(users, userInfos);
        var result = users.GroupBy(x => x.Id).ToDictionary(x => x.Key, x => x.First());
        return result;
    }

    public async Task<User?> GetUserById(string userId, CancellationToken cancellationToken)
    {
        var cached = await cache.Get<User>(UserByIdCacheKey(userId));
        if (cached == null)
        {
            cached = await userRepository.GetUserById(userId, cancellationToken);
            if (cached != null)
            {
                await cache.Put(UserByIdCacheKey(userId), cached);
            }
        }
        if (cached != null)
        {
            var userInfo = await GetUserInfo(userId, cancellationToken);
            if (userInfo != null)
            {
                cached.DisplayName = cached.DisplayName ?? userInfo.Name;
                cached.Role = userInfo.Role;
            }
        }
        return cached;
    }

    public async Task<Dictionary<string, UserInfo>> GetUserInfos(List<string> userIds, CancellationToken cancellationToken)
    {
        var cachedUserInfos = new List<UserInfo>();
        foreach (var userId in userIds)
        {
            var cached = await cache.Get<UserInfo>(UserInfoByIdCacheKey(userId));
            if (cached != null)
            {
                cachedUserInfos.Add(cached);
            }
        }
        var userIdsNotFoundInCache = userIds.Except(cachedUserInfos.Select(x => x.UserId)).ToList();
        var nonCached = await userRepository.GetUserInfos(userIdsNotFoundInCache, cancellationToken);
        foreach (var userInfo in nonCached)
        {
            await cache.Put(UserInfoByIdCacheKey(userInfo.UserId), userInfo);
        }
        var resultDict = new Dictionary<string, UserInfo>();
        foreach (var userInfo in cachedUserInfos)
        {
            resultDict[userInfo.UserId] = userInfo;
        }
        foreach (var userInfo in nonCached)
        {
            resultDict[userInfo.UserId] = userInfo;
        }
        return resultDict;
    }

    public async Task<UserInfo?> GetUserInfo(string userId, CancellationToken cancellationToken)
    {
        var cached = await cache.Get<UserInfo>(UserInfoByIdCacheKey(userId));
        if (cached == null)
        {
            cached = await userRepository.GetUserInfo(userId, cancellationToken);
            if (cached != null)
            {
                await cache.Put(UserInfoByIdCacheKey(userId), cached);
            }
        }
        return cached;
    }

    public async Task<User?> CreateUser(string email, string password, string displayName, CancellationToken cancellationToken)
    {
        var user = await userRepository.CreateUser(email, password, displayName, cancellationToken);
        await ClearCache();
        await SendConfirmEmail(email, includeWelcome: true);
        return user;
    }

    public async Task<User?> UpdateUser(string userId, string email, string displayName, string? password, Role role, CancellationToken cancellationToken)
    {
        var userBeforeUpdate = await GetUserById(userId, cancellationToken);
        if (userBeforeUpdate == null)
        {
            throw new GraphQLErrorException($"User with id {userId} not found");
        }
        try
        {
            await userRepository.UpdateUser(userId, email, displayName, password, cancellationToken);
            await userRepository.UpdateUserInfo(userId, new UserInfo
            {
                Name = displayName,
                Role = role,
            }, cancellationToken);
        }
        catch (FirebaseAuthException ex)
        {
            logger.LogError(ex, "failed to update user");
            throw new GraphQLErrorException(ex.Message);
        }
        await ClearCache(userId);
        var updatedUser = await GetUserById(userId, cancellationToken);
        if (updatedUser == null)
        {
            throw new GraphQLErrorException("failed to get updated user");
        }
        if (updatedUser != null && updatedUser.Email != userBeforeUpdate.Email)
        {
            await SendConfirmEmail(updatedUser.Email, includeWelcome: false);
        }
        return updatedUser;
    }

    private async Task SendConfirmEmail(string email, bool includeWelcome)
    {
        var verifyEmailLink = await FirebaseAuth.DefaultInstance.GenerateEmailVerificationLinkAsync(email);
        if (!string.IsNullOrEmpty(verifyEmailLink))
        {
            try
            {
                await emailService.SendEmail(email, "Bekræft email", $"{(includeWelcome ? "Velkommen til gastrik. " : "")}Bekræft venligst din email ved at trykke på linket: {verifyEmailLink}");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "failed to send email confirmation email to {to}", email);
            }
        }

    }

    public Task<VerifyPasswordResponse> SignIn(string email, string password, CancellationToken cancellationToken)
    {
        return userRepository.SignIn(email, password, cancellationToken);
    }

    public Task<RefreshTokenResponse> RefreshToken(string refreshToken, CancellationToken cancellationToken)
    {
        return userRepository.RefreshToken(refreshToken, cancellationToken);
    }

    private async Task ClearCache(string? userId = null)
    {
        await cache.Remove(GetUsersCacheKey);
        await cache.Remove(UserCountCacheKey);
        if (userId != null)
        {
            await cache.Remove(UserByIdCacheKey(userId));
            await cache.Remove(UserInfoByIdCacheKey(userId));
        }
    }

}