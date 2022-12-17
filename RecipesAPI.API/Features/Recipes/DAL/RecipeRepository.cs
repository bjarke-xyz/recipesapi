using Google.Cloud.Firestore;
using RecipesAPI.API.Exceptions;
using RecipesAPI.API.Features.Recipes.Common;

namespace RecipesAPI.API.Features.Recipes.DAL;

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
                if (dto.DeletedAt.HasValue)
                {
                    continue;
                }
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
            if (dto.DeletedAt.HasValue)
            {
                return null;
            }
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
        var snapshot = await db.Collection(recipeCollection).WhereEqualTo("deletedAt", null).WhereArrayContains("slugs", slug).Limit(1).GetSnapshotAsync(cancellationToken);
        if (snapshot.Count == 0)
        {
            return null;
        }
        var doc = snapshot.Documents.First();
        try
        {
            var dto = doc.ConvertTo<RecipeDto>();
            if (dto.DeletedAt.HasValue)
            {
                return null;
            }
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
            await db.Collection(recipeCollection).Document(recipe.Id).UpdateAsync("deletedAt", DateTime.UtcNow);
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
                if (dto.DeletedAt.HasValue)
                {
                    continue;
                }
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

    public async Task<RecipeStats> GetRecipeCount(bool published, bool moderated, CancellationToken cancellationToken)
    {
        var snapshot = await db.Collection(recipeCollection).WhereEqualTo("published", published).Select("createdByUser", "moderatedDateTime", "deletedAt").GetSnapshotAsync(cancellationToken);
        var recipeCount = 0;
        var userIds = new HashSet<string>();
        foreach (var doc in snapshot.Documents)
        {
            try
            {
                if (doc.TryGetValue<DateTime?>("deletedAt", out var deletedAt) && deletedAt.HasValue)
                {
                    continue;
                }
                if (moderated)
                {
                    if (!doc.TryGetValue<string?>("moderatedDateTime", out var moderatedAt) || string.IsNullOrEmpty(moderatedAt))
                    {
                        continue;
                    }
                }
                if (doc.TryGetValue<string>("createdByUser", out var userId))
                {
                    userIds.Add(userId);
                }
                recipeCount++;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "failed to get user id from doc {id} id", doc.Id);
            }
        }

        return new RecipeStats
        {
            RecipeCount = recipeCount,
            ChefCount = userIds.Count,
        };
    }

}