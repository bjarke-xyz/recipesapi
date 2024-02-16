
using RecipesAPI.API.Features.Food.BLL;
using RecipesAPI.API.Features.Food.Common;

namespace RecipesAPI.API.Features.Food.Graph;

[ExtendObjectType(OperationTypeNames.Query)]
public class FoodQueries
{
    public async Task<FoodItem?> Food([Service] FoodService foodService, CancellationToken cancellationToken, int id)
    {
        var foodItem = await foodService.GetFoodItem(id, cancellationToken);
        return foodItem;
    }
    public async Task<IEnumerable<FoodItem>> Foods([Service] FoodService foodService, CancellationToken cancellationToken, int? limit = null, int? skip = null)
    {
        var foodData = await foodService.GetFoodData(cancellationToken);
        if (foodData == null)
        {
            return [];
        }
        if (limit.HasValue && skip.HasValue)
        {
            return foodData.Skip(skip.Value).Take(limit.Value);
        }
        else if (limit.HasValue && !skip.HasValue)
        {
            return foodData.Take(limit.Value);
        }
        else if (!limit.HasValue && skip.HasValue)
        {
            return foodData.Skip(skip.Value);
        }
        else
        {
            return foodData;
        }
    }
}