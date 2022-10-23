using Google.Cloud.Firestore;

namespace RecipesAPI.Recipes.DAL;


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
    [FirestoreProperty("userId")]
    public string UserId { get; set; } = default!;
    [FirestoreProperty("createdDateTime")]
    public DateTime CreatedAt { get; set; }
    [FirestoreProperty("moderatedDateTime")]
    public DateTime? ModeratedAt { get; set; }
    [FirestoreProperty("lastModifiedDateTime")]
    public DateTime LastModifiedAt { get; set; }
    [FirestoreProperty("published")]
    public bool Published { get; set; }
    [FirestoreProperty("tips")]
    public List<string> Tips { get; set; } = default!;
    [FirestoreProperty("yield")]
    public string? Yield { get; set; }
    [FirestoreProperty("parts")]
    public List<RecipePartDto> Parts { get; set; } = default!;
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
}