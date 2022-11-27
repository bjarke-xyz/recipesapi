using RecipesAPI.API.Utils;
using RecipesAPI.API.Auth;
using RecipesAPI.API.Exceptions;
using RecipesAPI.API.Files.BLL;
using RecipesAPI.API.Food;
using RecipesAPI.API.Food.BLL;
using RecipesAPI.API.Food.Common;
using RecipesAPI.API.Recipes.BLL;
using RecipesAPI.API.Recipes.Common;
using RecipesAPI.API.Users.BLL;
using RecipesAPI.API.Users.Common;

namespace RecipesAPI.API.Recipes.Graph;

[ExtendObjectType(OperationTypeNames.Query)]
public class RecipeQueries
{
    public async Task<List<Recipe>> GetRecipes([User] User loggedInUser, [Service] RecipeService recipeService, CancellationToken cancellationToken, RecipeFilter? filter = null)
    {
        filter = filter ?? new RecipeFilter();
        var recipes = await recipeService.GetRecipes(cancellationToken, loggedInUser);

        recipes = recipes.Where(x => x.Published == (filter.Published ?? true)).ToList();
        recipes = recipes.Where(x => x.ModeratedAt.HasValue == (filter.IsModerated ?? true)).ToList();


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
    public Task<Recipe?> GetRecipe(string id, [User] User loggedInUser, [Service] RecipeService recipeService, CancellationToken cancellationToken)
    {
        return recipeService.GetRecipe(id, cancellationToken, loggedInUser);
    }

    public Task<Recipe?> GetRecipeBySlug(string slug, [User] User loggedInUser, [Service] RecipeService recipeService, CancellationToken cancellationToken)
    {
        return recipeService.GetRecipeBySlug(slug, loggedInUser, cancellationToken);
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
