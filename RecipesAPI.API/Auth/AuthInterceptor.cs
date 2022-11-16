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
        requestBuilder.SetProperty(UserAttribute.DictKey, user ?? new User());
        await base.OnCreateAsync(context, requestExecutor, requestBuilder, cancellationToken);
    }

}
