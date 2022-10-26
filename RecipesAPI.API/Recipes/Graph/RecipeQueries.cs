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
    private static string GetBaseUrl(HttpContext httpContext)
    {
        var scheme = httpContext.Request.Scheme;
        var host = httpContext.Request.Host.Host;
        var port = httpContext.Request.Host.Port;
        return $"{scheme}://{host}:{port ?? 443}";
    }
    public async Task<Image?> GetImage([Parent] Recipe recipe, [Service] IHttpContextAccessor contextAccessor, [Service] FileService fileService, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(recipe.ImageId)) return null;
        var file = await fileService.GetFile(recipe.ImageId, cancellationToken);
        if (file == null) return null;
        if (contextAccessor.HttpContext == null) return null;
        var baseUrl = GetBaseUrl(contextAccessor.HttpContext);
        return new Image
        {
            Name = file.FileName,
            Size = file.Size,
            Type = file.ContentType,
            Src = $"{baseUrl}/images/{file.Id}",
        };
    }
}