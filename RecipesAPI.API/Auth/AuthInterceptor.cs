using System.Security.Claims;
using HotChocolate.AspNetCore;
using HotChocolate.Execution;
using RecipesAPI.Users;
using RecipesAPI.Users.BLL;
using RecipesAPI.Users.Common;
using RecipesAPI.Users.DAL;

namespace RecipesAPI.Auth;

public class AuthInterceptor : DefaultHttpRequestInterceptor
{
    private readonly UserService userService;

    public AuthInterceptor(UserService userService)
    {
        this.userService = userService;
    }

    public override async ValueTask OnCreateAsync(HttpContext context, IRequestExecutor requestExecutor, IQueryRequestBuilder requestBuilder, CancellationToken cancellationToken)
    {
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        List<Role>? userRoles = null;
        if (!string.IsNullOrEmpty(userId))
        {
            var userInfo = await userService.GetUserInfo(userId, cancellationToken);
            if (userInfo != null)
            {
                var identity = new ClaimsIdentity();
                identity.AddClaims(userInfo.Roles.Select(role => new Claim(ClaimTypes.Role, role.ToString())));
                context.User.AddIdentity(identity);
                requestBuilder.SetProperty(UserIdAttribute.DictKey, userId);
                userRoles = userInfo.Roles;
            }
        }
        requestBuilder.SetProperty(UserRolesAttribute.DictKey, userRoles);
        await base.OnCreateAsync(context, requestExecutor, requestBuilder, cancellationToken);
    }

}
