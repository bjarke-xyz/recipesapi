using System.Net.Mail;
using System.Security.Claims;
using HotChocolate.AspNetCore.Authorization;
using RecipesAPI.API.Auth;
using RecipesAPI.API.Exceptions;
using RecipesAPI.API.Features.Recipes.BLL;
using RecipesAPI.API.Features.Users.BLL;
using RecipesAPI.API.Features.Users.Common;

namespace RecipesAPI.API.Features.Users.Graph;

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

    public async Task<bool> ResetPassword(string email, [Service] UserService userService, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(email) || !email.Contains("@"))
        {
            throw new GraphQLErrorException("Invalid email");
        }
        await userService.SendResetPasswordMail(email, cancellationToken);
        return true;
    }


    [RoleAuthorize(RoleEnums = new[] { Role.USER })]
    public async Task<User> UpdateMe(UpdateMeInput input, [User] User loggedInUser, [Service] UserService userService, [IdToken] string idToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(input.Email) && string.IsNullOrEmpty(input.DisplayName) && string.IsNullOrEmpty(input.Password))
        {
            throw new GraphQLErrorException("At least one field must be provided");
        }
        var existingUser = await userService.GetUserById(loggedInUser.Id, cancellationToken);
        if (existingUser == null)
        {
            throw new UserNotFoundException(loggedInUser.Id);
        }
        if (!string.IsNullOrEmpty(input.Email) && !IsEmailValid(input.Email))
        {
            throw new GraphQLErrorException("Email is invalid");
        }
        var email = input.Email ?? existingUser.Email;
        var displayName = input.DisplayName ?? existingUser.DisplayName ?? "";
        var password = input.Password;
        if (string.IsNullOrEmpty(password))
        {
            password = null;
        }
        var user = await userService.UpdateUser(loggedInUser.Id, email, displayName, password, existingUser.Role ?? Role.USER, idToken, cancellationToken);
        if (user == null)
        {
            throw new GraphQLErrorException("updated user was null");
        }
        return user;
    }

    [RoleAuthorize(RoleEnums = new[] { Role.ADMIN })]
    public async Task<User> UpdateUser(string userId, UpdateUserInput input, [Service] UserService userService, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(input.Email) && string.IsNullOrEmpty(input.DisplayName) && input.Role == null)
        {
            throw new GraphQLErrorException("At least one field must be provided");
        }
        var existingUser = await userService.GetUserById(userId, cancellationToken);
        if (existingUser == null)
        {
            throw new UserNotFoundException(userId);
        }
        if (!string.IsNullOrEmpty(input.Email) && !IsEmailValid(input.Email))
        {
            throw new GraphQLErrorException("Email is invalid");
        }

        var email = input.Email ?? existingUser.Email;
        var displayName = input.DisplayName ?? existingUser.DisplayName ?? "";
        var roles = input.Role ?? existingUser.Role ?? Role.USER;
        var updatedUser = await userService.UpdateUser(userId, email, displayName, null, roles, null, cancellationToken);
        if (updatedUser == null)
        {
            throw new GraphQLErrorException("updated user was null");
        }
        return updatedUser;
    }

    [RoleAuthorize(RoleEnums = new[] { Role.USER })]
    public async Task<List<string>> BookmarkRecipe(string recipeId, [User] User loggedInUser, [Service] RecipeService recipeService, [Service] UserService userService, CancellationToken cancellationToken)
    {
        var recipe = await recipeService.GetRecipe(recipeId, cancellationToken, loggedInUser) ?? throw new RecipeNotFoundException(recipeId);
        var bookmarkedRecipes = loggedInUser.BookmarkedRecipes ?? [];
        if (bookmarkedRecipes.Contains(recipe.Id))
        {
            return bookmarkedRecipes;
        }
        bookmarkedRecipes.Add(recipe.Id);
        await userService.SetBookmarkedRecipes(loggedInUser.Id, bookmarkedRecipes, cancellationToken);
        return bookmarkedRecipes;
    }

    [RoleAuthorize(RoleEnums = new[] { Role.USER })]
    public async Task<List<string>> UnbookmarkRecipe(string recipeId, [User] User loggedInUser, [Service] RecipeService recipeService, [Service] UserService userService, CancellationToken cancellationToken)
    {
        var recipe = await recipeService.GetRecipe(recipeId, cancellationToken, loggedInUser) ?? throw new RecipeNotFoundException(recipeId);
        var bookmarkedRecipes = loggedInUser.BookmarkedRecipes ?? [];
        if (!bookmarkedRecipes.Contains(recipe.Id))
        {
            return bookmarkedRecipes;
        }
        bookmarkedRecipes.Remove(recipeId);
        await userService.SetBookmarkedRecipes(loggedInUser.Id, bookmarkedRecipes, cancellationToken);
        return bookmarkedRecipes;
    }

    private bool IsEmailValid(string email)
    {
        try
        {
            new MailAddress(email);
            return true;
        }
        catch
        {
            return false;
        }
    }

}