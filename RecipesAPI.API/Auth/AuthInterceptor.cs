using System.Security.Claims;
using HotChocolate.AspNetCore;
using HotChocolate.Execution;
using RecipesAPI.API.Features.Users.BLL;
using RecipesAPI.API.Features.Users.Common;

namespace RecipesAPI.API.Auth;

public class AuthInterceptor : DefaultHttpRequestInterceptor
{
    private readonly UserService userService;

    public AuthInterceptor(UserService userService)
    {
        this.userService = userService;
    }

    public override async ValueTask OnCreateAsync(HttpContext context, IRequestExecutor requestExecutor, IQueryRequestBuilder requestBuilder, CancellationToken cancellationToken)
    {
        var userId = JwtUtil.GetUserId(context.User);
        User? user = null;
        if (!string.IsNullOrEmpty(userId))
        {
            user = await userService.GetUserById(userId, cancellationToken);
            if (user != null)
            {
                var identity = new ClaimsIdentity();
                var roles = new List<Role> { user.Role ?? Role.USER };
                if (RoleUtils.RoleHierarchy.TryGetValue(roles.FirstOrDefault(), out var subRoles))
                {
                    roles.AddRange(subRoles);
                }
                identity.AddClaims(roles.Select(role => new Claim(ClaimTypes.Role, role.ToString())));
                context.User.AddIdentity(identity);
            }
        }
        // requestBuilder.SetProperty(UserAttribute.DictKey, user ?? new User());
        requestBuilder.SetGlobalState(UserAttribute.DictKey, user ?? new User());
        var idToken = "";
        if (!string.IsNullOrEmpty(context.Request.Headers.Authorization.ToString()))
        {
            var parts = context.Request.Headers.Authorization.ToString().Split("Bearer ");
            if (parts.Length >= 2)
            {
                idToken = parts[1].Trim();
            }
        }
        // requestBuilder.SetProperty(IdTokenAttribute.DictKey, idToken);
        requestBuilder.SetGlobalState(IdTokenAttribute.DictKey, idToken);
        await base.OnCreateAsync(context, requestExecutor, requestBuilder, cancellationToken);
    }

}
