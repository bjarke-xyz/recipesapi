using System.Security.Claims;
using HotChocolate.AspNetCore.Authorization;
using RecipesAPI.Auth;
using RecipesAPI.Exceptions;
using RecipesAPI.Users.BLL;
using RecipesAPI.Users.Common;
using RecipesAPI.Users.DAL;

namespace RecipesAPI.Users.Graph;

[ExtendObjectType(OperationTypeNames.Query)]
public class UserQueries
{
    [Authorize]
    public async Task<User> GetMe([Service] UserService userService, [UserId] string userId, CancellationToken cancellationToken)
    {
        var user = await userService.GetUserById(userId, cancellationToken);
        if (user == null)
        {
            throw new GraphQLErrorException("User not found");
        }
        var userInfo = await userService.GetUserInfo(user.Id, cancellationToken);
        return user;
    }

    [RoleAuthorize(RoleEnums = new[] { Role.ADMIN })]
    public async Task<List<User>> GetUsers([Service] UserService userService, CancellationToken cancellationToken)
    {
        var users = await userService.GetUsers(cancellationToken);
        return users;
    }

}