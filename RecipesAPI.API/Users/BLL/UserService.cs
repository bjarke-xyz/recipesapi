using RecipesAPI.Admin.Common;
using RecipesAPI.Infrastructure;
using RecipesAPI.Users.Common;
using RecipesAPI.Users.DAL;

namespace RecipesAPI.Users.BLL;

public class UserService : ICacheKeyGetter
{
    private readonly DAL.UserRepository userRepository;
    private readonly ICacheProvider cache;

    public const string GetUsersCacheKey = "GetUsers";
    public string UserByIdCacheKey(string userId) => $"GetUserById:{userId}";
    public string UserInfoByIdCacheKey(string userId) => $"GetUserInfo:{userId}";

    public UserService(DAL.UserRepository userRepository, ICacheProvider cache)
    {
        this.userRepository = userRepository;
        this.cache = cache;
    }

    public CacheKeyInfo GetCacheKeyInfo()
    {
        return new CacheKeyInfo
        {
            CacheKeyPrefixes = new List<string>{
                GetUsersCacheKey,
                UserByIdCacheKey(""),
                UserInfoByIdCacheKey(""),
            },
            ResourceType = CachedResourceTypeHelper.USERS,
        };
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
        foreach (var user in cached)
        {
            if (userInfos.TryGetValue(user.Id, out var userInfo) && userInfo != null)
            {
                user.DisplayName = user.DisplayName ?? userInfo.Name;
                user.Role = userInfo.Roles.FirstOrDefault().ToString();
                user.Roles = userInfo.Roles;
            }
        }
        return cached;
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
        return user;
    }

    public async Task<User?> UpdateUser(string userId, string email, string displayName, CancellationToken cancellationToken)
    {
        var user = await userRepository.UpdateUser(userId, email, displayName, cancellationToken);
        await ClearCache(userId);
        return user;
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
        if (userId != null)
        {
            await cache.Remove(UserByIdCacheKey(userId));
            await cache.Remove(UserInfoByIdCacheKey(userId));
        }
    }

}