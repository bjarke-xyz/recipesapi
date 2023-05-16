namespace RecipesAPI.API.Auth;

public class UserAttribute : GlobalStateAttribute
{
    public const string DictKey = "User";
    public UserAttribute() : base(DictKey) { }
}

public class IdTokenAttribute : GlobalStateAttribute
{
    public const string DictKey = "IdToken";
    public IdTokenAttribute() : base(DictKey) { }
}