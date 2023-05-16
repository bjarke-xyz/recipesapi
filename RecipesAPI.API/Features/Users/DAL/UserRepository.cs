using System.Collections.ObjectModel;
using FirebaseAdmin.Auth;
using Google.Cloud.Firestore;
using Newtonsoft.Json;
using RecipesAPI.API.Exceptions;
using RecipesAPI.API.Features.Users.Common;

namespace RecipesAPI.API.Features.Users.DAL;

public class UserRepository
{
    private readonly string firebaseWebApiKey;
    private readonly HttpClient httpClient;
    private readonly FirestoreDb db;
    private readonly ILogger<UserRepository> logger;

    private const string usersCollection = "users";

    public UserRepository(string firebaseWebApiKey, FirestoreDb db, ILogger<UserRepository> logger)
    {
        this.firebaseWebApiKey = firebaseWebApiKey;
        this.db = db;
        httpClient = new HttpClient();
        this.logger = logger;
    }

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
            UserId = userInfo.UserId
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
            var resp = await httpClient.PostAsJsonAsync($"https://identitytoolkit.googleapis.com/v1/accounts:sendOobCode?key={firebaseWebApiKey}", payload, cancellationToken: cancellationToken);
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
            var resp = await httpClient.PostAsJsonAsync($"https://identitytoolkit.googleapis.com/v1/accounts:sendOobCode?key={firebaseWebApiKey}", payload);
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
        var count = await FirebaseAuth.DefaultInstance.ListUsersAsync(null).CountAsync();
        return count;
    }

    public async Task<List<User>> GetUsers(CancellationToken cancellationToken)
    {
        var users = new List<User>();
        var enumerator = FirebaseAuth.DefaultInstance.ListUsersAsync(null).GetAsyncEnumerator();
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
        var decodedToken = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(idToken, cancellationToken);
        return decodedToken.Uid;
    }

    public async Task<User?> GetUserById(string userId, CancellationToken cancellationToken)
    {
        var userRecord = await FirebaseAuth.DefaultInstance.GetUserAsync(userId, cancellationToken);
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

    public async Task<List<UserInfo>> GetUserInfos(List<string> userIds, CancellationToken cancellationToken)
    {
        if (userIds == null || userIds.Count == 0)
        {
            return new List<UserInfo>();
        }
        var snapshot = await db.Collection(usersCollection).WhereIn(FieldPath.DocumentId, userIds).GetSnapshotAsync();
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
        var userRecord = await FirebaseAuth.DefaultInstance.CreateUserAsync(args, cancellationToken);
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
        var userRecord = await FirebaseAuth.DefaultInstance.UpdateUserAsync(args);
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
            RequestUri = new Uri("https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key=" + this.firebaseWebApiKey),
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
            RequestUri = new Uri("https://securetoken.googleapis.com/v1/token?key=" + this.firebaseWebApiKey),
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
}