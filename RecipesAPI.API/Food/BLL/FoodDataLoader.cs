using RecipesAPI.API.Food.Common;

namespace RecipesAPI.API.Food.BLL;

public class FoodDataLoader : BatchDataLoader<string, List<FoodItem>>
{
    private readonly FoodService foodService;

    public FoodDataLoader(FoodService foodService, IBatchScheduler batchScheduler, DataLoaderOptions? options = null) : base(batchScheduler, options)
    {
        this.foodService = foodService;
    }

    protected override async Task<IReadOnlyDictionary<string, List<FoodItem>>> LoadBatchAsync(IReadOnlyList<string> keys, CancellationToken cancellationToken)
    {
        var foodData = await foodService.SearchFoodData(keys.ToList(), cancellationToken);
        return foodData;
    }
}