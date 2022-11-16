namespace RecipesAPI.Recipes.Graph;

public class RecipeInput
{
    public string Title { get; set; } = default!;
    public string? Description { get; set; }
    public IFile? Image { get; set; }
    public bool Published { get; set; }
    public List<string>? Tips { get; set; }
    public string? Yield { get; set; }
    public List<RecipePartInput>? Parts { get; set; }
}

public class RecipePartInput
{
    public string Title { get; set; } = default!;
    public List<RecipePartIngredientInput> Ingredients { get; set; } = default!;
    public List<string> Steps { get; set; } = default!;
}

public class RecipePartIngredientInput
{
    public string Original { get; set; } = default!;
    public int? Id { get; set; }
    public string? Title { get; set; }
    public double? Volume { get; set; }
    public string? Unit { get; set; }
    public List<string>? Meta { get; set; } = default!;
}

public class RecipeFilter
{
    public string? UserId { get; set; }
    public bool? Published { get; set; }

    public string? OrderByProperty { get; set; }
    public bool? OrderDesc { get; set; }

    public int? Skip { get; set; }
    public int? Limit { get; set; }
}