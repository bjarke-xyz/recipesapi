namespace RecipesAPI.Users.Graph;

public class SignInInput
{
    public string Email { get; set; } = default!;
    public string Password { get; set; } = default!;
}