using RecipesAPI.Admin.Common;
using RecipesAPI.Exceptions;
using RecipesAPI.Food;
using RecipesAPI.Infrastructure;
using RecipesAPI.Recipes.Common;
using RecipesAPI.Recipes.DAL;

namespace RecipesAPI.Recipes.BLL;

public class RecipeService : ICacheKeyGetter
{
    private readonly RecipeRepository recipeRepository;
    private readonly ICacheProvider cache;
    private readonly ParserService parserService;

    public const string GetRecipesCacheKey = "GetRecipes";
    public string GetRecipeCacheKey(string id) => $"GetRecipe:{id}";
    public string GetRecipeByTitleCacheKey(string title) => $"GetRecipeByTitle:{title}";
    public string GetRecipeByUserCacheKey(string userId) => $"GetRecipeByUser:{userId}";

    public RecipeService(RecipeRepository recipeRepository, ICacheProvider cache, ParserService parserService, FoodService foodService)
    {
        this.recipeRepository = recipeRepository;
        this.cache = cache;
        this.parserService = parserService;
    }

    public CacheKeyInfo GetCacheKeyInfo()
    {
        return new CacheKeyInfo
        {
            CacheKeyPrefixes = new List<string>
            {
                GetRecipesCacheKey,
                GetRecipeCacheKey(""),
                GetRecipeByTitleCacheKey(""),
                GetRecipeByUserCacheKey(""),
            },
            ResourceType = CachedResourceTypeHelper.RECIPES,
        };
    }

    private void EnrichIngredients(Recipe recipe)
    {
        foreach (var part in recipe.Parts)
        {
            foreach (var ingredient in part.Ingredients)
            {
                if (string.IsNullOrEmpty(ingredient.Title)) continue;
                var parsedIngredient = parserService.Parse(ingredient.Title);
                if (parsedIngredient != null)
                {
                    ingredient.Original = parsedIngredient.Original;
                    ingredient.Title = parsedIngredient.Title;
                    ingredient.Volume = parsedIngredient.Volume;
                    ingredient.Unit = parsedIngredient.Unit;
                    ingredient.Meta = parsedIngredient.Meta;
                }
            }
        }
    }

    public async Task<List<Recipe>> GetRecipes(CancellationToken cancellationToken, bool showUnpublished)
    {
        var cached = await cache.Get<List<Recipe>>(GetRecipesCacheKey);
        if (cached == null)
        {
            cached = await recipeRepository.GetRecipes(cancellationToken);
            await cache.Put<List<Recipe>>(GetRecipesCacheKey, cached);
        }
        if (!showUnpublished)
        {
            cached = cached.Where(x => x.Published).ToList();
        }
        foreach (var recipe in cached)
        {
            EnrichIngredients(recipe);
        }
        return cached;
    }

    public async Task<Recipe?> GetRecipe(string id, CancellationToken cancellationToken, bool showUnpublished, string? userId = null)
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
        if (cached != null && cached.Published == false && (!showUnpublished || cached.UserId != userId))
        {
            return null;
        }
        if (cached != null)
        {
            EnrichIngredients(cached);
        }
        return cached;
    }

    public async Task<Recipe?> GetRecipeByTitle(string title, CancellationToken cancellationToken, bool showUnpublished)
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
        if (cached?.Published == false && !showUnpublished)
        {
            return null;
        }
        if (cached != null)
        {
            EnrichIngredients(cached);
        }
        return cached;
    }

    public async Task<List<Recipe>> GetRecipesByUserId(string userId, CancellationToken cancellationToken, bool showUnpublished)
    {
        var cached = await cache.Get<List<Recipe>>(GetRecipeByUserCacheKey(userId));
        if (cached == null)
        {
            cached = await recipeRepository.GetRecipesByUserId(userId, cancellationToken);
            await cache.Put(GetRecipeByUserCacheKey(userId), cached);
        }
        if (!showUnpublished)
        {
            cached = cached.Where(x => x.Published).ToList();
        }
        foreach (var recipe in cached)
        {
            EnrichIngredients(recipe);
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
        var savedRecipe = await GetRecipe(recipe.Id, cancellationToken, true);
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
        var savedRecipe = await GetRecipe(recipe.Id, cancellationToken, true);
        if (savedRecipe == null)
        {
            throw new GraphQLErrorException("failed to get saved recipe");
        }
        return savedRecipe;
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