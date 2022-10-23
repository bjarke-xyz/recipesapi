namespace RecipesAPI.Users.Common;

public class User
{
    public string Id { get; set; } = default!;
    public string DisplayName { get; set; } = default!;
    public string Email { get; set; } = default!;
    public bool EmailVerified { get; set; }
}

public enum Role
{
    USER,
    ADMIN,
}

public class UserInfo
{
    public string UserId { get; set; } = default!;
    public List<Role> Roles { get; set; } = default!;
}

public class TokenPayload
{
    public string IdToken { get; set; } = default!;
    public string RefreshToken { get; set; } = default!;
}