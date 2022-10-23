namespace RecipesAPI.Recipes.Common;

public class Recipe
{
    public string Id { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string? Description { get; set; }
    public string? ImageId { get; set; }
    // public Image Image { get; set; }
    public string UserId { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
    public DateTime? ModeratedAt { get; set; }
    public DateTime LastModifiedAt { get; set; }
    public bool Published { get; set; }
    public List<string> Tips { get; set; } = default!;
    public string? Yield { get; set; }
    public List<RecipePart> Parts { get; set; } = default!;
}

public class RecipePart
{
    public string Title { get; set; } = default!;
    public List<RecipeIngredient> Ingredients { get; set; } = default!;
    public List<string> Steps { get; set; } = default!;
}

public class RecipeIngredient
{
    public string Original { get; set; } = default!;
    public int? Id { get; set; }
    public string? Title { get; set; }
    public double? Volume { get; set; }
    public string? Unit { get; set; }
    public List<string> Meta { get; set; } = default!;
}