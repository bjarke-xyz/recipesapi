namespace RecipesAPI.Auth;

public class UserIdAttribute : GlobalStateAttribute
{
    public const string DictKey = "UserId";
    public UserIdAttribute() : base(DictKey) { }
}

public class UserRolesAttribute : GlobalStateAttribute
{
    public const string DictKey = "UserRoles";
    public UserRolesAttribute() : base(DictKey) { }
}