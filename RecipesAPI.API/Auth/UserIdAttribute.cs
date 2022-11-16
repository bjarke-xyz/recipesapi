namespace RecipesAPI.Auth;

public class UserAttribute : GlobalStateAttribute
{
    public const string DictKey = "User";
    public UserAttribute() : base(DictKey) { }
}