using RecipesAPI.API.Utils;
using RecipesAPI.Auth;
using RecipesAPI.Exceptions;
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
    public async Task<List<Recipe>> GetRecipes([UserRoles] List<Role> userRoles, [Service] RecipeService recipeService, CancellationToken cancellationToken, RecipeFilter? filter = null)
    {
        filter = filter ?? new RecipeFilter();
        var recipes = await recipeService.GetRecipes(cancellationToken, (RoleUtils.IsModerator(userRoles)) && filter.Published == false);

        if (!string.IsNullOrEmpty(filter.OrderByProperty))
        {
            if (!ClassUtils.IsPropertyOf<Recipe>(filter.OrderByProperty, out var propertyInfo) || propertyInfo == null)
            {
                throw new GraphQLErrorException($"The property '{filter.OrderByProperty}' does not exist on Recipe");
            }
            recipes = recipes.AsQueryable().OrderBy(propertyInfo.Name, filter.OrderDesc ?? true).ToList();
        }

        recipes = recipes.Skip(filter.Skip ?? 0).Take(filter.Limit ?? recipes.Count).ToList();

        return recipes;
    }
    public Task<Recipe?> GetRecipe(string id, [UserId] string loggedInId, [UserRoles] List<Role> userRoles, [Service] RecipeService recipeService, CancellationToken cancellationToken)
    {
        return recipeService.GetRecipe(id, cancellationToken, RoleUtils.IsModerator(userRoles), userId: loggedInId);
    }

    public Task<Recipe?> GetRecipeBySlug(string slug, [UserRoles] List<Role> userRoles, [Service] RecipeService recipeService, CancellationToken cancellationToken)
    {
        return recipeService.GetRecipeBySlug(slug, cancellationToken, RoleUtils.IsModerator(userRoles));
    }

    [Obsolete("Use User.Recipes instead")]
    public Task<List<Recipe>> GetRecipesByUser(string userId, [UserId] string loggedInId, [UserRoles] List<Role> userRoles, [Service] RecipeService recipeService, CancellationToken cancellationToken)
    {
        return recipeService.GetRecipesByUserId(userId, cancellationToken, RoleUtils.IsModerator(userRoles) || userId == loggedInId);
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
    public async Task<Image?> GetImage([Parent] Recipe recipe, [Service] IFileService fileService, FileDataLoader fileDataLoader, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(recipe.ImageId)) return null;
        var file = await fileDataLoader.LoadAsync(recipe.ImageId, cancellationToken);
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

    public async Task<RecipeAuthor> GetUser([Parent] Recipe recipe, UserDataLoader userDataLoader, CancellationToken cancellationToken)
    {
        var user = await userDataLoader.LoadAsync(recipe.UserId, cancellationToken);
        return new RecipeAuthor
        {
            DisplayName = user.DisplayName ?? "",
            UserId = recipe.UserId,
        };
    }
}
