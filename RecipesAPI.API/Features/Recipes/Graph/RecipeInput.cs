using RecipesAPI.API.Features.Admin.Common;

namespace RecipesAPI.API.Features.Recipes.Graph;

public class RecipeInput
{
    public string Title { get; set; } = default!;
    public string? Description { get; set; }
    public string? FileCode { get; set; }
    [Obsolete("Not used. Use FileCode instead")]
    public IFile? Image { get; set; }
    public bool Published { get; set; }
    public List<string>? Tips { get; set; }
    public string? Yield { get; set; }
    public List<RecipePartInput>? Parts { get; set; }
    public string? Slug { get; set; }
    public List<string>? EquipmentIds { get; set; }

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
    public bool? ManuallyEntered { get; set; }
    public bool? Optional { get; set; }
    public List<AffiliateItemReference>? AffiliateItemReferences { get; set; }
}

public class RecipeFilter
{
    public string? UserId { get; set; }
    public bool? Published { get; set; }
    public bool? IsModerated { get; set; }

    public string? OrderByProperty { get; set; }
    public bool? OrderDesc { get; set; }

    public int? Skip { get; set; }
    public int? Limit { get; set; }
}

public class RecipeSearchInput
{
    public string SearchQuery { get; set; } = "";
    public bool? SearchPartsAndTips { get; set; } = null;
    public int? Skip { get; set; } = null;
    public int? Limit { get; set; } = null;
}

public class CreateUploadUrlInput
{
    public string ContentType { get; set; } = default!;
    public long ContentLength { get; set; } = default!;
    public string FileName { get; set; } = default!;
}

public class RateRecipeInput
{
    public int Score { get; set; }
    public string? Comment { get; set; }
}

public class CommentInput
{
    public string Message { get; set; } = "";
    public string? ParentCommentId { get; set; }
}