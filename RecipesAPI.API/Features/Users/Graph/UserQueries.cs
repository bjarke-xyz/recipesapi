using System.Security.Claims;
using HotChocolate.AspNetCore.Authorization;
using RecipesAPI.API.Auth;
using RecipesAPI.API.Exceptions;
using RecipesAPI.API.Features.Recipes.BLL;
using RecipesAPI.API.Features.Recipes.Common;
using RecipesAPI.API.Features.Users.BLL;
using RecipesAPI.API.Features.Users.Common;

namespace RecipesAPI.API.Features.Users.Graph;

[ExtendObjectType(OperationTypeNames.Query)]
public class UserQueries
{
    [Authorize]
    public User GetMe([Service] UserService userService, [User] User user, CancellationToken cancellationToken)
    {
        return user;
    }

    [RoleAuthorize(RoleEnums = new[] { Role.ADMIN })]
    public async Task<List<User>> GetUsers([Service] UserService userService, CancellationToken cancellationToken)
    {
        var users = await userService.GetUsers(cancellationToken);
        return users;
    }

    public async Task<SimpleUser?> GetSimpleUser([Service] UserService userService, CancellationToken cancellationToken, string userId)
    {
        var user = await userService.GetUserById(userId, cancellationToken);
        if (user == null)
        {
            return null;
        }
        var simpleUser = new SimpleUser
        {
            Id = user.Id,
            DisplayName = user.DisplayName,
        };
        return simpleUser;
    }

    public async Task<Stats> GetStats([Service] UserService userService, [Service] RecipeService recipeService, CancellationToken cancellationToken, bool showUnpublished = false, bool showModerated = true)
    {
        var userCount = await userService.GetUserCount(cancellationToken);
        var recipeStats = await recipeService.GetRecipeStats(!showUnpublished, showModerated, cancellationToken);

        return new Stats
        {
            UserCount = userCount,
            RecipeCount = recipeStats.RecipeCount,
            ChefCount = recipeStats.ChefCount,
        };
    }

}

[ExtendObjectType(typeof(User))]
public class ExtendedUserQueries
{
    public async Task<List<Recipe>> GetRecipes([Parent] User user, [User] User loggedInUser, [Service] RecipeService recipeService, CancellationToken cancellationToken)
    {
        var recipes = await recipeService.GetRecipesByUserId(user.Id, cancellationToken, loggedInUser.HasRole(Role.MODERATOR) || user.Id == loggedInUser.Id);
        return recipes;
    }
}

[ExtendObjectType(typeof(SimpleUser))]
public class ExtendedSimpleUserQueries
{
    public async Task<List<Recipe>> GetRecipes([Parent] SimpleUser simpleUser, [User] User loggedInUser, [Service] RecipeService recipeService, CancellationToken cancellationToken)
    {
        var recipes = await recipeService.GetRecipesByUserId(simpleUser.Id, cancellationToken, loggedInUser.HasRole(Role.MODERATOR) || simpleUser.Id == loggedInUser.Id);
        return recipes;
    }
}