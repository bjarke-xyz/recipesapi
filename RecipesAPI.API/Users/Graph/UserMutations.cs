using System.Security.Claims;
using HotChocolate.AspNetCore.Authorization;
using RecipesAPI.Auth;
using RecipesAPI.Exceptions;
using RecipesAPI.Users.BLL;
using RecipesAPI.Users.Common;
using RecipesAPI.Users.DAL;

namespace RecipesAPI.Users.Graph;

[ExtendObjectType(OperationTypeNames.Mutation)]
public class UserMutations
{
    public async Task<TokenPayload> SignUp(SignUpInput input, [Service] UserService userService, CancellationToken cancellationToken)
    {
        var user = await userService.CreateUser(input.Email, input.Password, input.DisplayName, cancellationToken);
        var signInResponse = await userService.SignIn(input.Email, input.Password, cancellationToken);
        if (signInResponse.Error != null)
        {
            throw new GraphQLErrorException(signInResponse.Error.Message);
        }
        return new TokenPayload
        {
            IdToken = signInResponse.IdToken,
            RefreshToken = signInResponse.RefreshToken,
        };
    }

    public async Task<TokenPayload> SignIn(SignInInput input, [Service] UserService userService, CancellationToken cancellationToken)
    {
        var signInResponse = await userService.SignIn(input.Email, input.Password, cancellationToken);
        if (signInResponse.Error != null)
        {
            throw new GraphQLErrorException(signInResponse.Error.Message);
        }
        return new TokenPayload
        {
            IdToken = signInResponse.IdToken,
            RefreshToken = signInResponse.RefreshToken,
        };
    }

    public async Task<TokenPayload> RefreshToken(string refreshToken, [Service] UserService userService, CancellationToken cancellationToken)
    {
        var refreshResponse = await userService.RefreshToken(refreshToken, cancellationToken);
        if (refreshResponse.Error != null)
        {
            throw new GraphQLErrorException(refreshResponse.Error.Message);
        }
        return new TokenPayload
        {
            IdToken = refreshResponse.IdToken,
            RefreshToken = refreshResponse.RefreshToken
        };
    }


    [RoleAuthorize(RoleEnums = new[] { Role.USER })]
    public async Task<User> UpdateMe(UpdateMeInput input, [UserId] string userId, [Service] UserService userService, CancellationToken cancellationToken)
    {
        var user = await userService.UpdateUser(userId, input.Email, input.DisplayName, cancellationToken);
        if (user == null)
        {
            throw new GraphQLErrorException("updated user was null");
        }
        return user;
    }

    // [RoleAuthorize(RoleEnums = new[] { Role.ADMIN })]
    // public Task<User> UpdateUser([Service] UserService userService, CancellationToken cancellationToken)
    // {
    //     throw new NotImplementedException("UpdateUser is not implemented");
    // }

}