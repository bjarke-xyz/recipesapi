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
        var queue = new PriorityQueue<FoodItem, int>();
        foreach (var item in foodData)
        {
            var (matched, rank) = HasMatch(item.FoodName.Da, query);
            if (matched)
            {
                queue.Enqueue(item, rank);
            }
            else
            {
                (matched, rank) = HasMatch(item.FoodName.En, query);
                if (matched)
                {
                    queue.Enqueue(item, rank);
                }
            }
        }
        var result = new List<FoodItem>();
        while (queue.Count > 0)
        {
            var potentialFood = queue.Dequeue();
            result.Add(potentialFood);
        }
        return result;
    }

    private (bool match, int rank) HasMatch(string foodName, string query)
    {
        var foodNameParts = foodName.Split(",", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        foreach (var foodNamePart in foodNameParts)
        {
            if (string.Equals(foodNamePart, query))
            {
                return (true, 0);
            }
            else if (string.Equals(foodNamePart, query, StringComparison.OrdinalIgnoreCase))
            {
                return (true, 10);
            }
            else if (foodNamePart.Contains(query))
            {
                return (true, 20);
            }
            else if (foodNamePart.Contains(query, StringComparison.OrdinalIgnoreCase))
            {
                return (true, 30);
            }
        }

        if (string.Equals(foodName, query))
        {
            return (true, 100);
        }
        else if (string.Equals(foodName, query, StringComparison.OrdinalIgnoreCase))
        {
            return (true, 200);
        }
        else if (foodName.Contains(query))
        {
            return (true, 300);
        }
        else if (foodName.Contains(query, StringComparison.OrdinalIgnoreCase))
        {
            return (true, 400);
        }
        return (false, 0);
    }
}