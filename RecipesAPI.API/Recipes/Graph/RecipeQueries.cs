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