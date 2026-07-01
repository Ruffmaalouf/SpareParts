namespace SpareParts.Api.Middleware;

/// <summary>
/// Adds baseline security response headers to every API response:
/// X-Content-Type-Options, X-Frame-Options, Referrer-Policy, a restrictive
/// Content-Security-Policy appropriate for a JSON API, and (over HTTPS) HSTS.
/// </summary>
public sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(static state =>
        {
            ApplyHeaders((HttpContext)state);
            return Task.CompletedTask;
        }, context);

        return _next(context);
    }

    /// <summary>Sets the baseline security headers on the given HTTP context's response. Exposed as a
    /// standalone static method so it can be unit tested directly, without depending on a real
    /// server's OnStarting invocation timing.</summary>
    public static void ApplyHeaders(HttpContext context)
    {
        var headers = context.Response.Headers;

        headers["X-Content-Type-Options"] = "nosniff";
        headers["X-Frame-Options"] = "DENY";
        headers["Referrer-Policy"] = "no-referrer";
        headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'";

        if (context.Request.IsHttps)
        {
            headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";
        }
    }
}
