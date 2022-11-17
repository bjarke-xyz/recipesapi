using System.Security.Claims;
using HotChocolate.AspNetCore.Authorization;
using RecipesAPI.Auth;
using RecipesAPI.Exceptions;
using RecipesAPI.Food;
using RecipesAPI.Food.BLL;
using RecipesAPI.Food.Common;
using RecipesAPI.Recipes.BLL;
using RecipesAPI.Recipes.Common;
using RecipesAPI.Users.BLL;
using RecipesAPI.Users.Common;

namespace RecipesAPI.Users.Graph;

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

    public async Task<Stats> GetStats([Service] UserService userService, [Service] RecipeService recipeService, CancellationToken cancellationToken, bool showUnpublished = false)
    {
        var users = await userService.GetUsers(cancellationToken);
        var recipes = await recipeService.GetRecipes(cancellationToken, null);

        if (!showUnpublished)
        {
            recipes = recipes.Where(x => x.Published).ToList();
        }

        var chefCount = recipes.Select(x => x.UserId).Distinct().Count();

        return new Stats
        {
            UserCount = users.Count,
            RecipeCount = recipes.Count,
            ChefCount = chefCount,
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