using Microsoft.AspNetCore.Http;
using SpareParts.Api.Middleware;

namespace SpareParts.ArchitectureTests;

public class SecurityHeadersMiddlewareTests
{
    [Fact]
    public void ApplyHeaders_ShouldSetBaselineSecurityHeaders()
    {
        var context = new DefaultHttpContext();
        context.Request.IsHttps = false;

        SecurityHeadersMiddleware.ApplyHeaders(context);

        Assert.Equal("nosniff", context.Response.Headers["X-Content-Type-Options"]);
        Assert.Equal("DENY", context.Response.Headers["X-Frame-Options"]);
        Assert.Equal("no-referrer", context.Response.Headers["Referrer-Policy"]);
        Assert.Contains("default-src 'none'", context.Response.Headers["Content-Security-Policy"].ToString());
    }

    [Fact]
    public void ApplyHeaders_OverHttps_ShouldSetHsts()
    {
        var context = new DefaultHttpContext();
        context.Request.IsHttps = true;

        SecurityHeadersMiddleware.ApplyHeaders(context);

        Assert.Contains("max-age=31536000", context.Response.Headers["Strict-Transport-Security"].ToString());
    }

    [Fact]
    public void ApplyHeaders_OverHttp_ShouldNotSetHsts()
    {
        var context = new DefaultHttpContext();
        context.Request.IsHttps = false;

        SecurityHeadersMiddleware.ApplyHeaders(context);

        Assert.Equal(string.Empty, context.Response.Headers["Strict-Transport-Security"].ToString());
    }

    [Fact]
    public async Task InvokeAsync_ShouldRegisterHeadersViaOnStarting_AndCallNext()
    {
        var nextInvoked = false;
        var middleware = new SecurityHeadersMiddleware(_ =>
        {
            nextInvoked = true;
            return Task.CompletedTask;
        });

        var context = new DefaultHttpContext();
        await middleware.InvokeAsync(context);

        Assert.True(nextInvoked);
    }
}
