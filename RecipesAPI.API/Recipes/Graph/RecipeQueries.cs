using RecipesAPI.Auth;
using RecipesAPI.Files.BLL;
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
    public Task<Recipe?> GetRecipe(string id, [UserId] string loggedInId, [UserRoles] List<Role> userRoles, [Service] RecipeService recipeService, CancellationToken cancellationToken) => recipeService.GetRecipe(id, cancellationToken, userRoles?.Contains(Role.ADMIN) ?? false, userId: loggedInId);
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

[ExtendObjectType(typeof(Recipe))]
public class ExtendedRecipeQueries
{
    public async Task<Image?> GetImage([Parent] Recipe recipe, [Service] FileService fileService, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(recipe.ImageId)) return null;
        var file = await fileService.GetFile(recipe.ImageId, cancellationToken);
        if (file == null) return null;
        var imageSrc = fileService.GetPublicUrl(file);
        return new Image
        {
            Name = file.FileName,
            Size = file.Size,
            Type = file.ContentType,
            Src = imageSrc,
        };
    }
}