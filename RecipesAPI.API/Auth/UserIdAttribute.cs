namespace RecipesAPI.API.Auth;

public class UserAttribute : GlobalStateAttribute
{
    public const string DictKey = "User";
    public UserAttribute() : base(DictKey) { }
}