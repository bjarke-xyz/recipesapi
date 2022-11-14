using RecipesAPI.Auth;
using RecipesAPI.Files.BLL;
using RecipesAPI.Food;
using RecipesAPI.Food.BLL;
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

    public Task<Recipe?> GetRecipeBySlug(string slug, [UserRoles] List<Role> userRoles, [Service] RecipeService recipeService, CancellationToken cancellationToken)
    {
        return recipeService.GetRecipeBySlug(slug, cancellationToken, userRoles?.Contains(Role.ADMIN) ?? false);
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
    public async Task<FoodItem?> GetFood([Parent] RecipeIngredient recipeIngredient, FoodDataLoader foodDataLoader, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(recipeIngredient.Title)) return null;

        var foodData = await foodDataLoader.LoadAsync(recipeIngredient.Title, cancellationToken);
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
        var displayName = user?.DisplayName;
        if (string.IsNullOrEmpty(displayName))
        {
            var userInfo = await userService.GetUserInfo(recipe.UserId, cancellationToken);
            displayName = userInfo?.Name;
        }
        if (string.IsNullOrEmpty(displayName))
        {
            displayName = "";
        }
        return new RecipeAuthor
        {
            DisplayName = displayName,
            UserId = recipe.UserId,
        };
    }
}
