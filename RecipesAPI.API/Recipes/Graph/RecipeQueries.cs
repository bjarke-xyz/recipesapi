using RecipesAPI.Food;
using RecipesAPI.Food.Common;
using RecipesAPI.Recipes.BLL;
using RecipesAPI.Recipes.Common;

namespace RecipesAPI.Recipes.Graph;

[ExtendObjectType(OperationTypeNames.Query)]
public class RecipeQueries
{
    public Task<List<Recipe>> GetRecipes([Service] RecipeService recipeService, CancellationToken cancellationToken) => recipeService.GetRecipes(cancellationToken);
    public Task<Recipe?> GetRecipe(string id, [Service] RecipeService recipeService, CancellationToken cancellationToken) => recipeService.GetRecipe(id, cancellationToken);
    public Task<Recipe?> GetRecipeByTitle(string title, [Service] RecipeService recipeService, CancellationToken cancellationToken) => recipeService.GetRecipeByTitle(title, cancellationToken);

}

[ExtendObjectType(typeof(RecipeIngredient))]
public class RecipeIngredientQueries
{
    public async Task<FoodItem?> GetFood([Parent] RecipeIngredient recipeIngredient, [Service] FoodService foodService, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(recipeIngredient.Title)) return null;

        // TODO: fix search
        var foodData = await foodService.SearchFoodData(recipeIngredient.Title, cancellationToken);
        return foodData.FirstOrDefault();
    }

}