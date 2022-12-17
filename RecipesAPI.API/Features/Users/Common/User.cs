namespace RecipesAPI.API.Features.Users.Common;

public class User
{
    public string Id { get; set; } = default!;
    public string? DisplayName { get; set; }
    public string Email { get; set; } = default!;
    public bool EmailVerified { get; set; }
    public Role? Role { get; set; } = default!;
    public List<Role> Roles => new List<Role> { this.Role ?? Common.Role.USER };

    public bool HasRole(Role role)
    {
        if (this.Role == null) return false;
        if (this.Role == role)
        {
            return true;
        }
        if (RoleUtils.RoleHierarchy.TryGetValue(this.Role.Value, out var subRoles))
        {
            if (subRoles.Contains(role))
            {
                return true;
            }
        }
        return false;
    }
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
    MODERATOR,
}

public static class RoleUtils
{
    public static readonly IReadOnlyDictionary<Role, IReadOnlyList<Role>> RoleHierarchy = new Dictionary<Role, IReadOnlyList<Role>>
    {
        { Role.ADMIN, new List<Role> { Role.MODERATOR, Role.USER } },
        { Role.MODERATOR, new List<Role> { Role.USER } },
    };
}

public class UserInfo
{
    public string UserId { get; set; } = default!;
    public Role Role { get; set; }
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

public class UserCount
{
    public int Count { get; set; }
}