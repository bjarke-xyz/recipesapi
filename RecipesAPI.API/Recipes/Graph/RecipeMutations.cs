using System.Security.Claims;
using RecipesAPI.Auth;
using RecipesAPI.Exceptions;
using RecipesAPI.Recipes.BLL;
using RecipesAPI.Recipes.Common;
using RecipesAPI.Users.Common;

namespace RecipesAPI.Recipes.Graph;

[ExtendObjectType(OperationTypeNames.Mutation)]
public class RecipeMutations
{
    [RoleAuthorize(RoleEnums = new[] { Role.USER })]
    public async Task<Recipe> CreateRecipe(RecipeInput input, [UserId] string userId, [Service] RecipeService recipeService, CancellationToken cancellationToken)
    {
        var recipe = RecipeMapper.MapInput(input);
        recipe.UserId = userId;
        var createdRecipe = await recipeService.CreateRecipe(recipe, cancellationToken);
        return createdRecipe;
    }

    [RoleAuthorize(RoleEnums = new[] { Role.USER })]
    public async Task<Recipe> UpdateRecipe(string id, RecipeInput input, [UserId] string userId, [UserRoles] List<Role> userRoles, [Service] RecipeService recipeService, CancellationToken cancellationToken)
    {
        var existingRecipe = await recipeService.GetRecipe(id, cancellationToken, true);
        if (existingRecipe == null)
        {
            throw new GraphQLErrorException($"recipe with id {id} not found");
        }
        if (existingRecipe.UserId != userId)
        {
            if (!userRoles.Contains(Role.ADMIN))
            {
                throw new GraphQLErrorException("You do not have permission to edit this recipe");
            }
        }

        var recipe = RecipeMapper.MapInput(input);
        recipe.Id = existingRecipe.Id;
        recipe.CreatedAt = existingRecipe.CreatedAt;
        recipe.UserId = existingRecipe.UserId;
        var updatedRecipe = await recipeService.UpdateRecipe(recipe, cancellationToken);
        return updatedRecipe;
    }

    public RecipeIngredient? ParseIngredient(string ingredient, [Service] ParserService parserService)
    {
        var parsedIngredient = parserService.Parse(ingredient);
        return parsedIngredient;
    }
}