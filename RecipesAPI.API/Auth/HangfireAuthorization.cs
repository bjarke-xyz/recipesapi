using System.Security.Claims;
using System.Text;
using Hangfire.Annotations;
using Hangfire.Dashboard;
using RecipesAPI.API.Features.Users.BLL;

namespace RecipesAPI.API.Auth;

public class HangfireDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize([NotNull] DashboardContext context)
    {
        if (context.GetHttpContext().Items.TryGetValue("HANGFIRE_AUTH_OK", out var authOk) && authOk is bool authOkBool)
        {
            return authOkBool;
        }
        return false;
    }
}

public class HangfireDashboardAuthorizationMiddleware : IMiddleware
{
    private readonly UserService userService;
    private readonly ILogger<HangfireDashboardAuthorizationMiddleware> logger;
    private readonly JwtUtil jwtUtil;

    private readonly string cookieKey = "auth";

    public HangfireDashboardAuthorizationMiddleware(UserService userService, ILogger<HangfireDashboardAuthorizationMiddleware> logger, JwtUtil jwtUtil)
    {
        this.userService = userService;
        this.logger = logger;
        this.jwtUtil = jwtUtil;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var authToken = context.Request.Cookies[cookieKey];
        var authHeader = context.Request.Headers.Authorization.ToString();
        if (string.IsNullOrEmpty(authToken) && string.IsNullOrEmpty(authHeader))
        {
            Unauthorized(context);
            return;
        }
        if (!string.IsNullOrEmpty(authToken))
        {
            try
            {
                var (isValid, _, claims) = await jwtUtil.ValidateToken(authToken);
                if (!isValid)
                {
                    Unauthorized(context);
                    return;
                }

                var userId = JwtUtil.GetUserId(claims);
                if (string.IsNullOrEmpty(userId))
                {
                    Unauthorized(context);
                    return;
                }
                var user = await userService.GetUserById(userId, context.RequestAborted);
                if (user == null)
                {
                    Unauthorized(context);
                    return;
                }

                if (!user.HasRole(Features.Users.Common.Role.ADMIN))
                {
                    Unauthorized(context);
                    return;
                }

            }
            catch (Exception ex)
            {
                logger.LogError(ex, "could not validate token");
                Unauthorized(context);
                return;
            }
        }
        if (string.IsNullOrEmpty(authToken) && !string.IsNullOrEmpty(authHeader))
        {
            try
            {
                var base64EncodedString = authHeader.Replace("Basic ", "");
                var decodedBytes = Convert.FromBase64String(base64EncodedString);
                var decodedStr = Encoding.UTF8.GetString(decodedBytes);
                var basicAuthParts = decodedStr.Split(":");
                if (basicAuthParts.Length != 2)
                {
                    Unauthorized(context);
                    return;
                }

                var passwdResponse = await userService.SignIn(basicAuthParts[0], basicAuthParts[1], context.RequestAborted);
                if (passwdResponse.Error != null)
                {
                    Unauthorized(context);
                    return;
                }

                var user = await userService.GetUserById(passwdResponse.LocalId, context.RequestAborted);
                if (user == null)
                {
                    Unauthorized(context);
                    return;
                }

                if (!user.HasRole(Features.Users.Common.Role.ADMIN))
                {
                    Unauthorized(context);
                    return;
                }

                context.Response.Cookies.Delete(cookieKey);
                context.Response.Cookies.Append(cookieKey, passwdResponse.IdToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "failed to do hangfire login");
                Unauthorized(context);
                return;
            }
        }

        context.Items["HANGFIRE_AUTH_OK"] = true;
        await next(context);
    }

    private void Unauthorized(HttpContext context)
    {
        context.Response.Cookies.Delete(cookieKey);
        context.Response.StatusCode = 401;
        context.Response.Headers.Add("WWW-Authenticate", "Basic realm=\"Hangfire\"");
    }
}

public static class HangfireAuthorizationExtensions
{
    public static IServiceCollection AddHangfireAuthorizationMiddleware(this IServiceCollection services)
    {
        return services.AddSingleton<HangfireDashboardAuthorizationMiddleware>();
    }
    public static IApplicationBuilder UseHangfireAuthorizationMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<HangfireDashboardAuthorizationMiddleware>();
    }
}