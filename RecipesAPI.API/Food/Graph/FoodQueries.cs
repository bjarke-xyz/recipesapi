using RecipesAPI.Food.Common;

namespace RecipesAPI.Food.Graph;

[ExtendObjectType(OperationTypeNames.Query)]
public class FoodQueries
{
    public async Task<IEnumerable<FoodItem>> Food([Service] FoodService foodService, CancellationToken cancellationToken, int? limit = null, int? skip = null)
    {
        var foodData = await foodService.GetFoodData(cancellationToken);
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