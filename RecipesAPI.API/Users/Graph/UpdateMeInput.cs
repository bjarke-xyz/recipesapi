namespace RecipesAPI.Users.Graph;

public class UpdateMeInput
{
    public string Email { get; set; } = default!;
    public string? Password { get; set; }
    public string DisplayName { get; set; } = default!;
}