using RecipesAPI.API.Auth;
using Sentry;

namespace RecipesAPI.API.Infrastructure;

public class MySentryUserFactory : ISentryUserFactory
{
    private readonly IHttpContextAccessor httpContextAccessor;

    public MySentryUserFactory(IHttpContextAccessor httpContextAccessor)
    {
        this.httpContextAccessor = httpContextAccessor;
    }

    public User? Create()
    {
        var user = httpContextAccessor.HttpContext?.User;
        if (user == null)
        {
            return null;
        }
        var userId = JwtUtil.GetUserId(user);
        if (string.IsNullOrEmpty(userId))
        {
            return null;
        }
        return new User
        {
            Id = userId,
            Username = userId,
        };
    }
}
