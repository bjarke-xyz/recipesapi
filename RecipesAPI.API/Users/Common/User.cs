namespace RecipesAPI.Users.Common;

public class User
{
    public string Id { get; set; } = default!;
    public string? DisplayName { get; set; }
    public string Email { get; set; } = default!;
    public bool EmailVerified { get; set; }
    public string Role { get; set; } = default!;
    public List<Role> Roles { get; set; } = default!;
}

public class SimpleUser
{
    public string Id { get; set; } = default!;
    public string? DisplayName { get; set; }
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
    public string? Name { get; set; } = null;
}

public class TokenPayload
{
    public string IdToken { get; set; } = default!;
    public string RefreshToken { get; set; } = default!;
}

public class Stats
{
    public int RecipeCount { get; set; }
    public int UserCount { get; set; }
    public int ChefCount { get; set; }
}