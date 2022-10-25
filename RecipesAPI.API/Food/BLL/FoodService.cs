using RecipesAPI.Food.Common;
using RecipesAPI.Food.DAL;
using RecipesAPI.Infrastructure;

namespace RecipesAPI.Food;

public class FoodService
{
    private readonly FoodRepository foodRepository;
    private readonly ICacheProvider cache;

    private List<FoodItem>? localCache = null;

    public const string FoodCacheKey = "GetFoodData";

    public FoodService(FoodRepository foodRepository, ICacheProvider cache)
    {
        this.foodRepository = foodRepository;
        this.cache = cache;
    }

    public async Task<List<FoodItem>> GetFoodData(CancellationToken cancellationToken)
    {
        var cached = localCache;
        if (cached == null)
        {
            cached = await cache.Get<List<FoodItem>>(FoodCacheKey);
            if (cached == null)
            {
                var dtos = await foodRepository.GetFoodData(cancellationToken);
                cached = FoodMapper.MapDtos(dtos);
                await cache.Put(FoodCacheKey, cached, TimeSpan.FromHours(24));
            }
            localCache = cached;
        }
        return cached;
    }

    public async Task<List<FoodItem>> SearchFoodData(string query, CancellationToken cancellationToken)
    {
        var foodData = await GetFoodData(cancellationToken);
        if (foodData == null) return new List<FoodItem>();
        var results = new List<FoodItem>();
        foreach (var item in foodData)
        {
            if (string.Equals(item.FoodName.Da, query, StringComparison.OrdinalIgnoreCase))
            {
                results.Add(item);
            }
            else if (string.Equals(item.FoodName.En, query, StringComparison.OrdinalIgnoreCase))
            {
                results.Add(item);
            }
            else if (item.FoodName.Da.Contains(query, StringComparison.InvariantCultureIgnoreCase))
            {
                results.Add(item);
            }
            else if (item.FoodName.En.Contains(query, StringComparison.InvariantCultureIgnoreCase))
            {
                results.Add(item);
            }
        }
        return results;
    }
}