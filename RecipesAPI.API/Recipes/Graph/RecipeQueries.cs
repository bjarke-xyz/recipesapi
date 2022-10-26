using RecipesAPI.Auth;
using RecipesAPI.Food;
using RecipesAPI.Food.Common;
using RecipesAPI.Recipes.BLL;
using RecipesAPI.Recipes.Common;
using RecipesAPI.Users.Common;

namespace RecipesAPI.Recipes.Graph;

[ExtendObjectType(OperationTypeNames.Query)]
public class RecipeQueries
{
    public Task<List<Recipe>> GetRecipes([UserRoles] List<Role> userRoles, [Service] RecipeService recipeService, CancellationToken cancellationToken) => recipeService.GetRecipes(cancellationToken, userRoles?.Contains(Role.ADMIN) ?? false);
    public Task<Recipe?> GetRecipe(string id, [UserRoles] List<Role> userRoles, [Service] RecipeService recipeService, CancellationToken cancellationToken) => recipeService.GetRecipe(id, cancellationToken, userRoles?.Contains(Role.ADMIN) ?? false);
    public Task<Recipe?> GetRecipeByTitle(string title, [UserRoles] List<Role> userRoles, [Service] RecipeService recipeService, CancellationToken cancellationToken) => recipeService.GetRecipeByTitle(title, cancellationToken, userRoles?.Contains(Role.ADMIN) ?? false);
    public Task<List<Recipe>> GetRecipesByUser(string userId, [UserId] string loggedInId, [UserRoles] List<Role> userRoles, [Service] RecipeService recipeService, CancellationToken cancellationToken) => recipeService.GetRecipesByUserId(userId, cancellationToken, (userRoles?.Contains(Role.ADMIN) ?? false) || userId == loggedInId);

    public RecipeIngredient? ParseIngredient(string ingredient, [Service] ParserService parserService)
    {
        var parsedIngredient = parserService.Parse(ingredient);
        return parsedIngredient;
    }
}

[ExtendObjectType(typeof(RecipeIngredient))]
public class RecipeIngredientQueries
{
    public async Task<FoodItem?> GetFood([Parent] RecipeIngredient recipeIngredient, [Service] FoodService foodService, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(recipeIngredient.Title)) return null;

        var foodData = await foodService.SearchFoodData(recipeIngredient.Title, cancellationToken);
        return foodData.FirstOrDefault();
    }

}