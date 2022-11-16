using RecipesAPI.Users.Common;

namespace RecipesAPI.Users.Graph;

public class UpdateMeInput
{
    public string? Email { get; set; } = default!;
    public string? Password { get; set; } = default!;
    public string? DisplayName { get; set; } = default!;
}

public class UpdateUserInput
{
    public string? Email { get; set; }
    public string? DisplayName { get; set; }
    public Role? Role { get; set; }
}