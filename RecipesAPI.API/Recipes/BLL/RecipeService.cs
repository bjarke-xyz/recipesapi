using RecipesAPI.Exceptions;
using RecipesAPI.Infrastructure;
using RecipesAPI.Recipes.Common;
using RecipesAPI.Recipes.DAL;

namespace RecipesAPI.Recipes.BLL;

public class RecipeService
{
    private readonly RecipeRepository recipeRepository;
    private readonly ICacheProvider cache;

    public const string GetRecipesCacheKey = "GetRecipes";
    public string GetRecipeCacheKey(string id) => $"GetRecipe:{id}";
    public string GetRecipeByTitleCacheKey(string title) => $"GetRecipeByTitle:{title}";
    public string GetRecipeByUserCacheKey(string userId) => $"GetRecipeByUser:{userId}";

    public RecipeService(RecipeRepository recipeRepository, ICacheProvider cache)
    {
        this.recipeRepository = recipeRepository;
        this.cache = cache;
    }

    public async Task<List<Recipe>> GetRecipes(CancellationToken cancellationToken)
    {
        var cached = await cache.Get<List<Recipe>>(GetRecipesCacheKey);
        if (cached == null)
        {
            cached = await recipeRepository.GetRecipes(cancellationToken);
            await cache.Put<List<Recipe>>(GetRecipesCacheKey, cached);
        }
        return cached;
    }

    public async Task<Recipe?> GetRecipe(string id, CancellationToken cancellationToken)
    {
        var cached = await cache.Get<Recipe>(GetRecipeCacheKey(id));
        if (cached == null)
        {
            cached = await recipeRepository.GetRecipe(id, cancellationToken);
            if (cached != null)
            {
                await cache.Put(GetRecipeCacheKey(id), cached);
            }
        }
        return cached;
    }

    public async Task<Recipe?> GetRecipeByTitle(string title, CancellationToken cancellationToken)
    {
        var cached = await cache.Get<Recipe>(GetRecipeByTitleCacheKey(title));
        if (cached == null)
        {
            cached = await recipeRepository.GetRecipeByTitle(title, cancellationToken);
            if (cached != null)
            {
                await cache.Put(GetRecipeByTitleCacheKey(title), cached);
            }
        }
        return cached;
    }

    public async Task<Recipe> CreateRecipe(Recipe recipe, CancellationToken cancellationToken)
    {
        var existingRecipe = await recipeRepository.GetRecipeByTitle(recipe.Title, cancellationToken);
        if (existingRecipe != null)
        {
            throw new GraphQLErrorException($"recipe with name '{recipe.Title}' already exists");
        }
        var id = Guid.NewGuid().ToString();
        recipe.Id = id;
        await recipeRepository.SaveRecipe(recipe, cancellationToken);
        await ClearCache();
        var savedRecipe = await GetRecipe(recipe.Id, cancellationToken);
        if (savedRecipe == null)
        {
            throw new GraphQLErrorException("failed to get saved recipe");
        }
        return savedRecipe;
    }

    public async Task<Recipe> UpdateRecipe(Recipe recipe, CancellationToken cancellationToken)
    {
        await recipeRepository.SaveRecipe(recipe, cancellationToken);
        await ClearCache(recipe.Id, recipe.Title, recipe.UserId);
        var savedRecipe = await GetRecipe(recipe.Id, cancellationToken);
        if (savedRecipe == null)
        {
            throw new GraphQLErrorException("failed to get saved recipe");
        }
        return savedRecipe;
    }

    public async Task<List<Recipe>> GetRecipesByUserId(string userId, CancellationToken cancellationToken)
    {
        var cached = await cache.Get<List<Recipe>>(GetRecipeByUserCacheKey(userId));
        if (cached == null)
        {
            cached = await recipeRepository.GetRecipesByUserId(userId, cancellationToken);
            await cache.Put(GetRecipeByUserCacheKey(userId), cached);
        }
        return cached;
    }

    private async Task ClearCache(string? recipeId = null, string? recipeTitle = null, string? userId = null)
    {
        await cache.Remove(GetRecipesCacheKey);
        if (recipeId != null)
        {
            await cache.Remove(GetRecipeCacheKey(recipeId));
        }
        if (recipeTitle != null)
        {
            await cache.Remove(GetRecipeByTitleCacheKey(recipeTitle));
        }
        if (userId != null)
        {
            await cache.Remove(GetRecipeByUserCacheKey(userId));
        }
    }
}