using RecipesAPI.Food.Common;
using RecipesAPI.Food.DAL;
using RecipesAPI.Infrastructure;

namespace RecipesAPI.Food.BLL;

public class FoodService
{
    private readonly FoodRepository foodRepository;
    private readonly ICacheProvider cache;

    private IReadOnlyList<FoodItem>? localCache = null;
    private Dictionary<int, FoodItem>? localCacheDict = null;

    public FoodService(FoodRepository foodRepository, ICacheProvider cache)
    {
        this.foodRepository = foodRepository;
        this.cache = cache;
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

        if (query.Contains(" "))
        {
            var queryParts = query.Split(" ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Reverse().ToList();
            var spaceRank = 1;
            foreach (var queryPart in queryParts)
            {
                var (matched, rank) = HasMatch(foodName, queryPart);
                if (matched)
                {
                    return (true, rank + spaceRank);
                }
                spaceRank++;
            }
        }

        return (false, int.MaxValue);
    }
}