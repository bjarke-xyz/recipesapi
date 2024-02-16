using System.Text.RegularExpressions;
using RecipesAPI.API.Features.Food.Common;
using RecipesAPI.API.Features.Food.DAL;
using RecipesAPI.API.Features.Recipes.Common;
using RecipesAPI.API.Infrastructure;

namespace RecipesAPI.API.Features.Food.BLL;

public class FoodService
{
    private bool loadingFoodData = false;
    private readonly FoodRepository foodRepository;
    private readonly FoodSearchServiceV1 foodSearchServiceV1;
    private readonly FoodSearchServiceV2 foodSearchServiceV2;

    private IReadOnlyList<FoodItem>? localCache = null;
    private Dictionary<int, FoodItem>? localCacheDict = null;

    private Dictionary<string, string> commonFoodNameReplacements = new Dictionary<string, string>
    {
        { "mel", "hvedemel"}
    };

    public FoodService(FoodRepository foodRepository, FoodSearchServiceV2 foodSearchServiceV2, FoodSearchServiceV1 foodSearchServiceV1)
    {
        this.foodRepository = foodRepository;
        this.foodSearchServiceV2 = foodSearchServiceV2;
        this.foodSearchServiceV1 = foodSearchServiceV1;
    }

    public string GetSearchQuery(RecipeIngredient recipeIngredient)
    {
        var query = recipeIngredient.Title;
        if (recipeIngredient.Meta != null && recipeIngredient.Meta.Any())
        {
            var percentage = recipeIngredient.Meta.FirstOrDefault(x => x.Contains("%"));
            if (!string.IsNullOrEmpty(percentage))
            {
                query = $"{query} {percentage}";
            }
        }
        return query ?? "";
    }

    public async Task BuildSearchIndex(CancellationToken cancellationToken)
    {
        var foodData = await GetFoodData(cancellationToken);
        if (foodData != null)
        {
            foodSearchServiceV2.IndexData(foodData);
        }
    }

    public async Task<IReadOnlyList<FoodItem>?> GetFoodData(CancellationToken cancellationToken)
    {
        await PopulateLocalCache(cancellationToken);
        return localCache;
    }

    public async Task<FoodItem?> GetFoodItem(int id, CancellationToken cancellationToken)
    {
        await PopulateLocalCache(cancellationToken);
        if (localCacheDict?.TryGetValue(id, out var foodItem) == true)
        {
            return foodItem;
        }
        return null;
    }

    private async Task PopulateLocalCache(CancellationToken cancellationToken)
    {
        if (loadingFoodData) return;
        loadingFoodData = true;
        var cached = localCache;
        if (cached == null)
        {
            var dtos = await foodRepository.GetFoodData(cancellationToken);
            cached = FoodMapper.MapDtos(dtos);
            localCache = cached;
        }
        if (localCacheDict == null)
        {
            localCacheDict = cached.GroupBy(x => x.FoodId).ToDictionary(x => x.Key, x => x.First());
        }
        loadingFoodData = false;
    }

    public async Task<Dictionary<string, List<FoodItem>>> SearchFoodData(List<string> queries, CancellationToken cancellationToken)
    {
        var foodData = await GetFoodData(cancellationToken);
        if (foodData == null) return new Dictionary<string, List<FoodItem>>();
        var result = new Dictionary<string, List<FoodItem>>();
        foreach (var query in queries)
        {
            var searchResult = await SearchFoodData(query, cancellationToken);
            result[query] = searchResult;
        }
        return result;
    }

    public async Task<List<FoodItem>> SearchFoodData(string query, CancellationToken cancellationToken)
    {
        if (commonFoodNameReplacements.TryGetValue(query, out var replacementQuery))
        {
            query = replacementQuery;
        }
        var foodData = await GetFoodData(cancellationToken);
        if (foodData == null) return new List<FoodItem>();
        List<FoodItemSearchDoc> searchResults;
        var version = 1;
        if (version == 1) searchResults = foodSearchServiceV1.Search(foodData, query);
        else searchResults = foodSearchServiceV2.Search(query);
        var foodItems = new List<FoodItem>();
        foreach (var result in searchResults)
        {
            if (localCacheDict?.TryGetValue(result.FoodId, out var foodItem) == true)
            {
                foodItems.Add(foodItem);
            }
        }
        return foodItems;
    }


}