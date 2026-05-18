using System.Security.Claims;
using SpareParts.Api.Infrastructure;

namespace SpareParts.Api.Middleware;

public sealed class WebAppUserRestrictionMiddleware
{
    private static readonly string[] AllowedApiPrefixes =
    [
        "/api/auth/me",
        "/api/auth/external-login",
        "/api/web-catalog",
        "/hubs/notifications"
    ];

    private readonly RequestDelegate _next;

    public WebAppUserRestrictionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (HttpMethods.IsOptions(context.Request.Method))
        {
            await _next(context);
            return;
        }

        var role = context.User.FindFirst(ClaimTypes.Role)?.Value;
        if (!string.Equals(role, WebAppUserRoleMigration.RoleName, StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        var path = context.Request.Path.Value ?? string.Empty;
        var allowed = AllowedApiPrefixes.Any(prefix =>
            path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

        if (allowed)
        {
            await _next(context);
            return;
        }

        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new
        {
            message = "Web app users can only browse available parts, manage their cart, and checkout."
        });
    }
}
