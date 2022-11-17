using Google.Cloud.Firestore;
using RecipesAPI.Exceptions;
using RecipesAPI.Recipes.Common;

namespace RecipesAPI.Recipes.DAL;

public class RecipeRepository
{
    private readonly ILogger<RecipeRepository> logger;
    private readonly FirestoreDb db;

    private const string recipeCollection = "recipes";

    public RecipeRepository(FirestoreDb db, ILogger<RecipeRepository> logger)
    {
        this.db = db;
        this.logger = logger;
    }

    public async Task<bool> IsSlugUnique(string slug, string? recipeId = null, CancellationToken cancellationToken = default)
    {
        var query = db.Collection(recipeCollection).Select(FieldPath.DocumentId).WhereArrayContains("slugs", slug);
        if (!string.IsNullOrEmpty(recipeId))
        {
            query = query.WhereNotEqualTo(FieldPath.DocumentId, recipeId);
        }
        var snapshot = await query.GetSnapshotAsync(cancellationToken);
        return snapshot.Count == 0;
    }

    public async Task<List<Recipe>> GetRecipes(CancellationToken cancellationToken)
    {
        var snapshot = await db.Collection(recipeCollection).GetSnapshotAsync(cancellationToken);
        var recipes = new List<Recipe>();
        foreach (var doc in snapshot.Documents)
        {
            try
            {
                var dto = doc.ConvertTo<RecipeDto>();
                var recipe = RecipeMapper.MapDto(dto, doc.Id);
                recipes.Add(recipe);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "failed to convert firebase doc {id} to dto", doc.Id);
            }
        }
        return recipes;
    }

    public async Task<Recipe?> GetRecipe(string id, CancellationToken cancellationToken)
    {
        var doc = await db.Collection(recipeCollection).Document(id).GetSnapshotAsync(cancellationToken);
        if (!doc.Exists)
        {
            return null;
        }
        try
        {
            var dto = doc.ConvertTo<RecipeDto>();
            return RecipeMapper.MapDto(dto, doc.Id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "failed to convert firebase doc {id} to dto", doc.Id);
            throw;
        }
    }

    public async Task<Recipe?> GetRecipeBySlug(string slug, CancellationToken cancellationToken)
    {
        var snapshot = await db.Collection(recipeCollection).WhereArrayContains("slugs", slug).Limit(1).GetSnapshotAsync(cancellationToken);
        if (snapshot.Count == 0)
        {
            return null;
        }
        var doc = snapshot.Documents.First();
        try
        {
            var dto = doc.ConvertTo<RecipeDto>();
            return RecipeMapper.MapDto(dto, doc.Id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "failed to convert firebase doc {id} to dto", doc.Id);
            throw;
        }
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
        await db.Collection(recipeCollection).Document(dto.Id).SetAsync(dto, null, cancellationToken);
    }

    public async Task DeleteRecipe(Recipe recipe, CancellationToken cancellationToken)
    {
        try
        {
            await db.Collection(recipeCollection).Document(recipe.Id).DeleteAsync(null, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "failed to delete recipe with id {id}", recipe.Id);
            throw;
        }
    }

    public async Task<List<Recipe>> GetRecipesByUserId(string userId, CancellationToken cancellationToken)
    {
        var snapshot = await db.Collection(recipeCollection).WhereEqualTo("createdByUser", userId).GetSnapshotAsync(cancellationToken);
        var recipes = new List<Recipe>();
        foreach (var doc in snapshot.Documents)
        {
            try
            {
                var dto = doc.ConvertTo<RecipeDto>();
                var recipe = RecipeMapper.MapDto(dto, doc.Id);
                recipes.Add(recipe);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "failed to convert firebase doc {id} to dto", doc.Id);
            }
        }
        return recipes;
    }

}