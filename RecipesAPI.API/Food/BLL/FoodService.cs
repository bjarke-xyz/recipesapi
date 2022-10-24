using RecipesAPI.Food.Common;
using RecipesAPI.Food.DAL;
using RecipesAPI.Infrastructure;

namespace RecipesAPI.Food;

public class FoodService
{
    private readonly FoodRepository foodRepository;
    private readonly ICacheProvider cache;

    public const string FoodCacheKey = "GetFoodData";

    public FoodService(FoodRepository foodRepository, ICacheProvider cache)
    {
        this.foodRepository = foodRepository;
        this.cache = cache;
    }

    public async Task<List<FoodItem>> GetFoodData(CancellationToken cancellationToken)
    {
        var cached = await cache.Get<List<FoodItem>>(FoodCacheKey);
        if (cached == null)
        {
            var dtos = await foodRepository.GetFoodData(cancellationToken);
            cached = FoodMapper.MapDtos(dtos);
            await cache.Put(FoodCacheKey, cached, TimeSpan.FromHours(24));
        }
        return cached;
    }
}