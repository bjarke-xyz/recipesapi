using System.Collections.ObjectModel;
using FirebaseAdmin.Auth;
using Google.Cloud.Firestore;
using Newtonsoft.Json;
using RecipesAPI.API.Exceptions;
using RecipesAPI.API.Features.Users.Common;

namespace RecipesAPI.API.Features.Users.DAL;

public class UserRepository(string firebaseWebApiBaseUrl, string firebaseWebApiKey, FirestoreDb db, FirebaseAuth auth, ILogger<UserRepository> logger)
{
    private readonly string firebaseWebApiBaseUrl = firebaseWebApiBaseUrl;
    private readonly string firebaseWebApiKey = firebaseWebApiKey;
    private readonly HttpClient httpClient = new HttpClient();
    private readonly FirestoreDb db = db;
    private readonly FirebaseAuth auth = auth;
    private readonly ILogger<UserRepository> logger = logger;

    private const string usersCollection = "users";

    private User? MapUserRecord(UserRecord? userRecord)
    {
        if (userRecord == null) return null;
        return new User
        {
            Id = userRecord.Uid,
            DisplayName = userRecord.DisplayName,
            Email = userRecord.Email,
            EmailVerified = userRecord.EmailVerified,
        };
    }

    private UserInfo? MapDto(UserInfoDto? dto)
    {
        if (dto == null) return null;
        var userInfo = new UserInfo
        {
            UserId = dto.UserId,
            Name = dto.Name,
            BookmarkedRecipes = dto.BookmarkedRecipes ?? [],
        };
        if (Enum.TryParse<Role>(dto.Role.ToUpper(), out var role))
        {
            userInfo.Role = role;
        }
        return userInfo;
    }

    private UserInfoDto? MapDto(UserInfo? userInfo)
    {
        if (userInfo == null) return null;
        var dto = new UserInfoDto
        {
            Name = userInfo.Name,
            Role = userInfo.Role.ToString().ToUpper(),
            UserId = userInfo.UserId,
            BookmarkedRecipes = userInfo.BookmarkedRecipes ?? [],
        };
        return dto;
    }

    public async Task SendResetPasswordMail(string email, CancellationToken cancellationToken)
    {
        try
        {
            var payload = new
            {
                requestType = "PASSWORD_RESET",
                email = email,
            };
            var resp = await httpClient.PostAsJsonAsync($"{firebaseWebApiBaseUrl}/v1/accounts:sendOobCode?key={firebaseWebApiKey}", payload, cancellationToken: cancellationToken);
            if (!resp.IsSuccessStatusCode)
            {
                var respBody = await resp.Content.ReadAsStringAsync();
                logger.LogError("send password reset mail failed: {status}: {body}", resp.StatusCode, respBody);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "failed to send reset password to {email}", email);
        }
    }

    public async Task SendConfirmEmail(string idToken)
    {
        try
        {
            var payload = new
            {
                requestType = "VERIFY_EMAIL",
                idToken = idToken
            };
            var resp = await httpClient.PostAsJsonAsync($"{firebaseWebApiBaseUrl}/v1/accounts:sendOobCode?key={firebaseWebApiKey}", payload);
            if (!resp.IsSuccessStatusCode)
            {
                var respBody = await resp.Content.ReadAsStringAsync();
                logger.LogError("send password reset mail failed: {status}: {body}", resp.StatusCode, respBody);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "failed to send email confirmation mail to {idToken}", idToken);
        }
    }

    public async Task<int> GetUserCount(CancellationToken cancellationToken)
    {
        var count = await auth.ListUsersAsync(null).CountAsync();
        return count;
    }

    public async Task<List<User>> GetUsers(CancellationToken cancellationToken)
    {
        var users = new List<User>();
        var enumerator = auth.ListUsersAsync(null).GetAsyncEnumerator(cancellationToken);
        while (await enumerator.MoveNextAsync())
        {
            ExportedUserRecord userRecord = enumerator.Current;
            var user = MapUserRecord(userRecord);
            if (user != null) users.Add(user);
        }
        return users;
    }

    public async Task<string> GetUserIdFromToken(string idToken, CancellationToken cancellationToken)
    {
        var decodedToken = await auth.VerifyIdTokenAsync(idToken, cancellationToken);
        return decodedToken.Uid;
    }

    public async Task<User?> GetUserById(string userId, CancellationToken cancellationToken)
    {
        var userRecord = await auth.GetUserAsync(userId, cancellationToken);
        return MapUserRecord(userRecord);
    }

    public async Task<UserInfo?> GetUserInfo(string userId, CancellationToken cancellationToken)
    {
        var docRef = db.Collection(usersCollection).Document(userId);
        var snapshot = await docRef.GetSnapshotAsync();
        if (!snapshot.Exists)
        {
            return null;
        }
        var dto = snapshot.ConvertTo<UserInfoDto>();
        if (string.IsNullOrEmpty(dto.UserId))
        {
            dto.UserId = docRef.Id;
        }
        return MapDto(dto);
    }

    public async Task SyncUsers(List<string>? userIds, CancellationToken cancellationToken)
    {
        // TODO: if userIds?.Count > 0, only get those users from auth
        var userRecords = await GetUsers(cancellationToken);
        var userRecordIdentifiers = userRecords.Select(x => x.Id);
        var userRecordIdsSet = userRecordIdentifiers.ToHashSet();
        var userInfos = await GetUserInfos(userRecordIdentifiers.ToList(), cancellationToken);
        foreach (var uf in userInfos)
        {
            if (userRecordIdsSet.Contains(uf.UserId))
            {
                userRecordIdsSet.Remove(uf.UserId);
            }
        }

        // create missing
        foreach (var userId in userRecordIdsSet)
        {
            var userInfo = CreateDefaultUserInfo(userId);
            await this.UpdateUserInfo(userId, userInfo, cancellationToken);
        }
    }

    private static UserInfo CreateDefaultUserInfo(string userId)
    {
        return new UserInfo
        {
            BookmarkedRecipes = [],
            Name = "",
            Role = Role.USER,
            UserId = userId,
        };

    }

    public async Task<UserInfo> GetUserInfoOrDefault(string userId, CancellationToken cancellationToken)
    {
        var userInfo = await GetUserInfo(userId, cancellationToken);
        if (userInfo == null)
        {
            userInfo = CreateDefaultUserInfo(userId);
        }
        return userInfo;
    }

    public async Task<List<UserInfo>> GetUserInfos(CancellationToken cancellationToken)
    {
        var snapshot = await db.Collection(usersCollection).GetSnapshotAsync(cancellationToken);
        var userInfos = new List<UserInfo>();
        foreach (var doc in snapshot.Documents)
        {
            var dto = doc.ConvertTo<UserInfoDto>();
            if (string.IsNullOrEmpty(dto.UserId))
            {
                dto.UserId = doc.Id;
            }
            var userInfo = MapDto(dto);
            if (userInfo != null)
            {
                userInfos.Add(userInfo);
            }
        }
        return userInfos;
    }

    public async Task<List<UserInfo>> GetUserInfos(List<string> userIds, CancellationToken cancellationToken)
    {
        if (userIds == null || userIds.Count == 0)
        {
            return new List<UserInfo>();
        }
        var snapshot = await db.Collection(usersCollection).WhereIn(FieldPath.DocumentId, userIds).GetSnapshotAsync(cancellationToken);
        var userInfos = new List<UserInfo>();
        foreach (var doc in snapshot.Documents)
        {
            var dto = doc.ConvertTo<UserInfoDto>();
            if (string.IsNullOrEmpty(dto.UserId))
            {
                dto.UserId = doc.Id;
            }
            var userInfo = MapDto(dto);
            if (userInfo != null)
            {
                userInfos.Add(userInfo);
            }
        }
        return userInfos;
    }

    public async Task<User?> CreateUser(string email, string password, string displayName, CancellationToken cancellationToken)
    {
        var args = new UserRecordArgs
        {
            Email = email,
            Password = password,
            DisplayName = displayName,
        };
        var userRecord = await auth.CreateUserAsync(args, cancellationToken);
        var userInfo = new UserInfoDto
        {
            UserId = userRecord.Uid,
            Role = Role.USER.ToString().ToUpper(),
            Name = displayName,
        };
        await db.Collection(usersCollection).Document(userInfo.UserId).SetAsync(userInfo, null, cancellationToken);

        return MapUserRecord(userRecord);
    }

    public async Task<User?> UpdateUser(string userId, string email, string displayName, string? password, CancellationToken cancellationToken)
    {
        var args = new UserRecordArgs
        {
            Uid = userId,
            Email = email,
            DisplayName = displayName,
        };
        if (!string.IsNullOrEmpty(password))
        {
            args.Password = password;
        }
        var userRecord = await auth.UpdateUserAsync(args);
        return MapUserRecord(userRecord);
    }

    public async Task UpdateUserInfo(string userId, UserInfo userInfo, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(userInfo);
        if (string.IsNullOrEmpty(userInfo.UserId))
        {
            userInfo.UserId = userId;
        }
        var dto = MapDto(userInfo);
        await db.Collection(usersCollection).Document(userId).SetAsync(dto, null, cancellationToken);
    }

    public async Task SetBookmarkedRecipes(string userId, List<string> recipeIds, CancellationToken cancellationToken)
    {
        var existingUser = await GetUserInfoOrDefault(userId, cancellationToken);
        existingUser.BookmarkedRecipes = recipeIds;
        var dto = MapDto(existingUser);
        await db.Collection(usersCollection).Document(userId).SetAsync(dto, cancellationToken: cancellationToken);
    }

    public async Task<VerifyPasswordResponse> SignIn(string email, string password, CancellationToken cancellationToken)
    {
        var requestBody = new Dictionary<string, object> {
            { "email", email},
            {"password", password},
            {"returnSecureToken", true}
        };
        var requestBodyContent = new StringContent(JsonConvert.SerializeObject(requestBody), System.Text.Encoding.UTF8, "application/json");
        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri("{firebaseWebApiBaseUrl}/v1/accounts:signInWithPassword?key=" + firebaseWebApiKey),
            Content = requestBodyContent,
        };
        var response = await httpClient.SendAsync(request, cancellationToken);
        var responseStr = await response.Content.ReadAsStringAsync();
        var responseObj = JsonConvert.DeserializeObject<VerifyPasswordResponse>(responseStr);
        if (responseObj == null)
        {
            throw new GraphQLErrorException("Failed to deserialize sign in response");
        }
        return responseObj;
    }

    public async Task<RefreshTokenResponse> RefreshToken(string refreshToken, CancellationToken cancellationToken)
    {
        var requestBody = new Dictionary<string, object> {
            { "grant_type", "refresh_token"},
            {"refresh_token", refreshToken}
        };
        var requestBodyContent = new StringContent(JsonConvert.SerializeObject(requestBody), System.Text.Encoding.UTF8, "application/json");
        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri("https://securetoken.googleapis.com/v1/token?key=" + firebaseWebApiKey),
            Content = requestBodyContent,
        };
        var response = await httpClient.SendAsync(request, cancellationToken);
        var responseStr = await response.Content.ReadAsStringAsync();
        var responseObj = JsonConvert.DeserializeObject<RefreshTokenResponse>(responseStr);
        if (responseObj == null)
        {
            throw new GraphQLErrorException("Failed to deserialize refresh token response");
        }
        return responseObj;
    }
}

public class AuthError
{
    public int Code { get; set; }
    public string Message { get; set; } = default!;
}
public class VerifyPasswordResponse
{
    public string IdToken { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string LocalId { get; set; } = default!;
    public string RefreshToken { get; set; } = default!;
    public AuthError? Error { get; set; }
}

public class RefreshTokenResponse
{
    [JsonProperty("refresh_token")]
    public string RefreshToken { get; set; } = default!;

    [JsonProperty("id_token")]
    public string IdToken { get; set; } = default!;

    [JsonProperty("error")]
    public AuthError? Error { get; set; }
}

[FirestoreData]
public class UserInfoDto
{
    [FirestoreProperty("userId")]
    public string UserId { get; set; } = default!;

    [FirestoreProperty("role")]
    public string Role { get; set; } = default!;

    [FirestoreProperty("name")]
    public string? Name { get; set; } = null;

    [FirestoreProperty("bookmarkedRecipes")]
    public List<string> BookmarkedRecipes { get; set; } = [];
}