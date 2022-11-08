using RecipesAPI.Auth;
using RecipesAPI.Files.BLL;
using RecipesAPI.Food;
using RecipesAPI.Food.Common;
using RecipesAPI.Recipes.BLL;
using RecipesAPI.Recipes.Common;
using RecipesAPI.Users.BLL;
using RecipesAPI.Users.Common;

namespace RecipesAPI.Recipes.Graph;

[ExtendObjectType(OperationTypeNames.Query)]
public class RecipeQueries
{
    public Task<List<Recipe>> GetRecipes([UserRoles] List<Role> userRoles, [Service] RecipeService recipeService, CancellationToken cancellationToken, RecipeFilter? filter = null)
    {
        filter = filter ?? new RecipeFilter();
        return recipeService.GetRecipes(cancellationToken, (userRoles?.Contains(Role.ADMIN) ?? false) && filter.Published == false);
    }
    public Task<Recipe?> GetRecipe(string id, [UserId] string loggedInId, [UserRoles] List<Role> userRoles, [Service] RecipeService recipeService, CancellationToken cancellationToken)
    {
        return recipeService.GetRecipe(id, cancellationToken, userRoles?.Contains(Role.ADMIN) ?? false, userId: loggedInId);
    }

    [Obsolete]
    public Task<Recipe?> GetRecipeByTitle(string title, [UserRoles] List<Role> userRoles, [Service] RecipeService recipeService, CancellationToken cancellationToken)
    {
        return recipeService.GetRecipeByTitle(title, cancellationToken, userRoles?.Contains(Role.ADMIN) ?? false);
    }

    [Obsolete("Use User.Recipes instead")]
    public Task<List<Recipe>> GetRecipesByUser(string userId, [UserId] string loggedInId, [UserRoles] List<Role> userRoles, [Service] RecipeService recipeService, CancellationToken cancellationToken)
    {
        return recipeService.GetRecipesByUserId(userId, cancellationToken, (userRoles?.Contains(Role.ADMIN) ?? false) || userId == loggedInId);
    }

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
    public async Task<Image?> GetImage([Parent] Recipe recipe, [Service] IFileService fileService, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(recipe.ImageId)) return null;
        var file = await fileService.GetFile(recipe.ImageId, cancellationToken);
        if (file == null) return null;
        var imageSrc = fileService.GetPublicUrl(file);
        var image = new Image
        {
            ImageId = recipe.ImageId,
            Name = file.FileName,
            Size = file.Size,
            Type = file.ContentType,
            Src = imageSrc,
            BlurHash = file.BlurHash,
            Dimensions = RecipeMapper.MapDto(file.Dimensions),
        };
        return image;
    }

    public async Task<RecipeAuthor> GetUser([Parent] Recipe recipe, [Service] UserService userService, CancellationToken cancellationToken)
    {
        var user = await userService.GetUserById(recipe.UserId, cancellationToken);
        var name = user?.DisplayName;
        if (string.IsNullOrEmpty(name))
        {
            var userInfo = await userService.GetUserInfo(recipe.UserId, cancellationToken);
            name = userInfo?.Name;
        }
        if (string.IsNullOrEmpty(name))
        {
            name = "";
        }
        return new RecipeAuthor
        {
            Name = name,
            UserId = recipe.UserId,
        };
    }
}
