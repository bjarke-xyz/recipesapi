using Google.Cloud.Firestore;
using RecipesAPI.API.Features.Admin.DAL;

namespace RecipesAPI.API.Features.Recipes.DAL;


[FirestoreData]
public class RecipeDto
{
    [FirestoreProperty("id")]
    public string Id { get; set; } = default!;
    [FirestoreProperty("title")]
    public string Title { get; set; } = default!;
    [FirestoreProperty("description")]
    public string? Description { get; set; }
    [FirestoreProperty("imageId")]
    public string? ImageId { get; set; }
    [FirestoreProperty("createdByUser")]
    public string UserId { get; set; } = default!;
    [FirestoreProperty("createdDateTime")]
    public string CreatedAt { get; set; } = default!;
    // public DateTime CreatedAt { get; set; }
    [FirestoreProperty("moderatedDateTime")]
    public string? ModeratedAt { get; set; }
    // public DateTime? ModeratedAt { get; set; }
    [FirestoreProperty("lastModifiedDateTime")]
    public string LastModifiedAt { get; set; } = default!;
    // public DateTime LastModifiedAt { get; set; }
    [FirestoreProperty("published")]
    public bool Published { get; set; }
    [FirestoreProperty("tips")]
    public List<string> Tips { get; set; } = default!;
    [FirestoreProperty("yield")]
    public string? Yield { get; set; }
    [FirestoreProperty("parts")]
    public List<RecipePartDto> Parts { get; set; } = default!;
    [FirestoreProperty("Difficulty")]
    public int? Difficulty { get; set; }
    [FirestoreProperty("slugs")]
    public List<string> Slugs { get; set; } = default!;

    [FirestoreProperty("deletedAt")]
    public DateTime? DeletedAt { get; set; }

    [FirestoreProperty("draft")]
    public RecipeDto? Draft { get; set; } = default!;

    [FirestoreProperty("equipmentIds")]
    public List<string> EquipmentIds { get; set; } = new List<string>();

    [FirestoreProperty("rating")]
    public RecipeRatingDto? Rating { get; set; } = null;
}

[FirestoreData]
public class RecipePartDto
{
    [FirestoreProperty("title")]
    public string Title { get; set; } = default!;
    [FirestoreProperty("ingredients")]
    public List<RecipeIngredientDto> Ingredients { get; set; } = default!;
    [FirestoreProperty("steps")]
    public List<string> Steps { get; set; } = default!;
    [FirestoreProperty("timeEstimate")]
    public double? TimeEstimate { get; set; }
}

[FirestoreData]
public class RecipeIngredientDto
{
    [FirestoreProperty("original")]
    public string Original { get; set; } = default!;
    [FirestoreProperty("id")]
    public int? Id { get; set; }
    [FirestoreProperty("title")]
    public string? Title { get; set; }
    [FirestoreProperty("volume")]
    public double? Volume { get; set; }
    [FirestoreProperty("unit")]
    public string? Unit { get; set; }
    [FirestoreProperty("meta")]
    public List<string> Meta { get; set; } = default!;
    [FirestoreProperty("manuallyEntered")]
    public bool ManuallyEntered { get; set; }
    [FirestoreProperty("optional")]
    public bool Optional { get; set; }

    [FirestoreProperty("affiliateItemReferences")]
    public List<AffiliateItemReferenceDto> AffiliateItemReferences { get; set; } = [];
}

[FirestoreData]
public class RecipeRatingDto
{
    [FirestoreProperty("score")]
    public double Score { get; set; }
    [FirestoreProperty("raters")]
    public int Raters { get; set; }
}