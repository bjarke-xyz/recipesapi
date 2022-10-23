using Google.Cloud.Firestore;
using RecipesAPI.Exceptions;
using RecipesAPI.Recipes.Common;

namespace RecipesAPI.Recipes.DAL;

public class RecipeRepository
{
    private readonly FirestoreDb db;

    private const string recipeCollection = "recipes";

    public RecipeRepository(FirestoreDb db)
    {
        this.db = db;
    }

    public async Task<List<Recipe>> GetRecipes(CancellationToken cancellationToken)
    {
        var snapshot = await db.Collection(recipeCollection).GetSnapshotAsync();
        var recipes = new List<Recipe>();
        foreach (var doc in snapshot.Documents)
        {
            var dto = doc.ConvertTo<RecipeDto>();
            var recipe = RecipeMapper.MapDto(dto);
            recipes.Add(recipe);
        }
        return recipes;
    }

    public async Task<Recipe?> GetRecipe(string id, CancellationToken cancellationToken)
    {
        var doc = await db.Collection(recipeCollection).Document(id).GetSnapshotAsync();
        if (!doc.Exists)
        {
            return null;
        }
        var dto = doc.ConvertTo<RecipeDto>();
        return RecipeMapper.MapDto(dto);
    }

    public async Task<Recipe?> GetRecipeByTitle(string title, CancellationToken cancellationToken)
    {
        var snapshot = await db.Collection(recipeCollection).WhereEqualTo("title", title).Limit(1).GetSnapshotAsync();
        if (snapshot.Count == 0)
        {
            return null;
        }
        var doc = snapshot.Documents.First();
        var dto = doc.ConvertTo<RecipeDto>();
        return RecipeMapper.MapDto(dto);
    }

    public async Task SaveRecipe(Recipe recipe, CancellationToken cancellationToken)
    {
        if (recipe.CreatedAt == DateTime.MinValue)
        {
            recipe.CreatedAt = DateTime.UtcNow;
        }
        if (recipe.LastModifiedAt == DateTime.MinValue)
        {
            recipe.LastModifiedAt = DateTime.UtcNow;
        }
        var dto = RecipeMapper.Map(recipe);
        await db.Collection(recipeCollection).Document(dto.Id).SetAsync(dto);
    }

    public async Task<List<Recipe>> GetRecipesByUserId(string userId, CancellationToken cancellationToken)
    {
        var snapshot = await db.Collection(recipeCollection).WhereEqualTo("userId", userId).GetSnapshotAsync();
        var recipes = new List<Recipe>();
        foreach (var doc in snapshot.Documents)
        {
            var dto = doc.ConvertTo<RecipeDto>();
            var recipe = RecipeMapper.MapDto(dto);
            recipes.Add(recipe);
        }
        return recipes;
    }

}