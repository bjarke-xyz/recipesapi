using RecipesAPI.Admin.Common;
using RecipesAPI.API.Utils;
using RecipesAPI.Exceptions;
using RecipesAPI.Files.BLL;
using RecipesAPI.Food;
using RecipesAPI.Food.BLL;
using RecipesAPI.Infrastructure;
using RecipesAPI.Recipes.Common;
using RecipesAPI.Recipes.DAL;
using RecipesAPI.Users.Common;
using SixLabors.ImageSharp.Processing;

namespace RecipesAPI.Recipes.BLL;

public class RecipeService : ICacheKeyGetter
{
    private readonly RecipeRepository recipeRepository;
    private readonly ICacheProvider cache;
    private readonly ParserService parserService;
    private readonly ILogger<RecipeService> logger;

    public const string GetRecipesCacheKey = "GetRecipes";
    public string GetRecipeCacheKey(string id) => $"GetRecipe:{id}";
    public string GetRecipeByUserCacheKey(string userId) => $"GetRecipeByUser:{userId}";
    public string GetRecipeBySlugCacheKey(string slug) => $"GetRecipeBySlug:{slug}";
    public string RecipeStatsCacheKey(bool published) => $"RecipeStats:{published}";

    public RecipeService(RecipeRepository recipeRepository, ICacheProvider cache, ParserService parserService, FoodService foodService, ILogger<RecipeService> logger)
    {
        this.recipeRepository = recipeRepository;
        this.cache = cache;
        this.parserService = parserService;
        this.logger = logger;
    }

    public CacheKeyInfo GetCacheKeyInfo()
    {
        return new CacheKeyInfo
        {
            CacheKeyPrefixes = new List<string>
            {
                GetRecipesCacheKey,
                GetRecipeCacheKey(""),
                GetRecipeByUserCacheKey(""),
                GetRecipeBySlugCacheKey(""),
                RecipeStatsCacheKey(true),
                RecipeStatsCacheKey(false),
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
                if (string.IsNullOrEmpty(ingredient.Title) && string.IsNullOrEmpty(ingredient.Original)) continue;
                var parsedIngredient = parserService.Parse(ingredient.Title ?? ingredient.Original);
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

    private bool IsRecipeVisible(Recipe? recipe, User? loggedInUser)
    {
        if (recipe == null || recipe.Published || loggedInUser == null)
        {
            return true;
        }

        if (loggedInUser.HasRole(Role.MODERATOR))
        {
            return true;
        }
        if (loggedInUser.Id == recipe.UserId)
        {
            return true;
        }
        return false;
    }

    public async Task<RecipeStats> GetRecipeStats(bool published, CancellationToken cancellationToken)
    {
        var recipeStats = await cache.Get<RecipeStats>(RecipeStatsCacheKey(published));
        if (recipeStats == null)
        {
            recipeStats = await recipeRepository.GetRecipeCount(published, cancellationToken);
            await cache.Put(RecipeStatsCacheKey(published), recipeStats, TimeSpan.FromDays(7), cancellationToken);
        }
        return recipeStats;
    }

    public async Task<List<Recipe>> GetRecipes(CancellationToken cancellationToken, User? loggedInUser)
    {
        var cached = await cache.Get<List<Recipe>>(GetRecipesCacheKey);
        if (cached == null)
        {
            cached = await recipeRepository.GetRecipes(cancellationToken);
            await cache.Put<List<Recipe>>(GetRecipesCacheKey, cached);
        }
        cached = cached.Where(x => IsRecipeVisible(x, loggedInUser)).ToList();
        foreach (var recipe in cached)
        {
            EnrichIngredients(recipe);
        }
        return cached;
    }

    public async Task<Recipe?> GetRecipe(string id, CancellationToken cancellationToken, User? loggedInUser)
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
        if (!IsRecipeVisible(cached, loggedInUser))
        {
            return null;
        }
        if (cached != null)
        {
            EnrichIngredients(cached);
        }
        return cached;
    }

    public async Task<Recipe?> GetRecipeByTitle(string title, CancellationToken cancellationToken)
    {
        var recipes = await GetRecipes(cancellationToken, null);
        var recipe = recipes.FirstOrDefault(x => string.Equals(x.Title, title, StringComparison.OrdinalIgnoreCase));
        return recipe;
    }

    public async Task<Recipe?> GetRecipeBySlug(string slug, User loggedInUser, CancellationToken cancellationToken)
    {
        var cached = await cache.Get<Recipe>(GetRecipeBySlugCacheKey(slug));
        if (cached == null)
        {
            cached = await recipeRepository.GetRecipeBySlug(slug, cancellationToken);
            if (cached != null)
            {
                await cache.Put(GetRecipeBySlugCacheKey(slug), cached);
            }
        }
        if (!IsRecipeVisible(cached, loggedInUser))
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
        var id = Guid.NewGuid().ToString();
        recipe.Id = id;
        if (!string.IsNullOrEmpty(recipe.Slug))
        {
            var isSlugUnique = await recipeRepository.IsSlugUnique(recipe.Slug, null, cancellationToken);
            if (!isSlugUnique)
            {
                throw new GraphQLErrorException($"Slug '{recipe.Slug}' is in use");
            }
        }
        await recipeRepository.SaveRecipe(recipe, cancellationToken);
        await ClearCache(null, recipe.UserId);
        var savedRecipe = await GetRecipe(recipe.Id, cancellationToken, null);
        if (savedRecipe == null)
        {
            throw new GraphQLErrorException("failed to get saved recipe");
        }
        return savedRecipe;
    }

    public async Task<Recipe> UpdateRecipe(Recipe recipe, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(recipe.Slug))
        {
            var isSlugUnique = await recipeRepository.IsSlugUnique(recipe.Slug, recipe.Id, cancellationToken);
            if (!isSlugUnique)
            {
                throw new GraphQLErrorException($"Slug '{recipe.Slug}' is in use");
            }
        }
        await recipeRepository.SaveRecipe(recipe, cancellationToken);
        await ClearCache(recipe.Id, recipe.UserId, recipe.Slugs);
        var savedRecipe = await GetRecipe(recipe.Id, cancellationToken, null);
        if (savedRecipe == null)
        {
            throw new GraphQLErrorException("failed to get saved recipe");
        }
        return savedRecipe;
    }

    public async Task DeleteRecipe(Recipe recipe, CancellationToken cancellationToken)
    {
        await recipeRepository.DeleteRecipe(recipe, cancellationToken);
    }

    private async Task ClearCache(string? recipeId = null, string? userId = null, List<string>? slugs = null)
    {
        await cache.Remove(GetRecipesCacheKey);
        await cache.Remove(RecipeStatsCacheKey(true));
        await cache.Remove(RecipeStatsCacheKey(false));
        if (recipeId != null)
        {
            await cache.Remove(GetRecipeCacheKey(recipeId));
        }
        if (userId != null)
        {
            await cache.Remove(GetRecipeByUserCacheKey(userId));
        }
        if (slugs != null)
        {
            foreach (var slug in slugs)
            {
                await cache.Remove(GetRecipeBySlugCacheKey(slug));
            }
        }
    }

}