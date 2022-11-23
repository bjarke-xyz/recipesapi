namespace RecipesAPI.API.Users.Graph;

public class SignUpInput
{
    public string Email { get; set; } = default!;
    public string Password { get; set; } = default!;
    public string DisplayName { get; set; } = default!;
}