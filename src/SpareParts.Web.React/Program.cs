var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

app.UseHttpsRedirection();

// Security response headers.
//
// The app is a hand-written UMD/static-file SPA (no bundler) that legitimately talks to:
//  - unpkg.com                    : React/ReactDOM/SignalR/qrcode-generator UMD scripts
//  - fonts.googleapis.com/gstatic : Google Fonts stylesheet + font files
//  - accounts.google.com          : Google Identity Services (GSI) script + OAuth popup/XHR flow
//  - connect.facebook.net         : Facebook JS SDK (loaded dynamically only when a Facebook App Id
//                                   is configured via config.js)
//  - the configured API origin    : REST calls (fetch) and SignalR websocket notifications
//
// NOTE: the API base URL is intentionally user-configurable at the login screen (see
// js/core/stores.js / app-shell.js) so operators can point the same static web build at a
// self-hosted or per-tenant API. A strict CSP cannot allow "any URL the user types in a text
// box" - that is a fundamental tension between this feature and connect-src allow-listing.
// This policy allows the two origins shipped in config.js/config.staging.js (localhost:5000 for
// local dev, the Azure staging API) plus https:/wss: as a pragmatic fallback so a user-supplied
// HTTPS API origin still works. If a stricter policy is required later, the API origin should
// become a build-time value instead of a runtime-editable one.
app.Use(async (context, next) =>
{
    var headers = context.Response.Headers;

    const string csp =
        "default-src 'self'; " +
        "script-src 'self' https://unpkg.com https://accounts.google.com https://connect.facebook.net; " +
        "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com; " +
        "font-src 'self' https://fonts.gstatic.com; " +
        "img-src 'self' data: blob: https:; " +
        "connect-src 'self' http://localhost:5000 https://spareparts-api-ralph.azurewebsites.net " +
        "https://accounts.google.com https://www.facebook.com https://connect.facebook.net https: wss: ws:; " +
        "frame-src https://accounts.google.com https://www.facebook.com; " +
        "object-src 'none'; " +
        "base-uri 'self'; " +
        "form-action 'self'; " +
        "frame-ancestors 'none'";

    headers["Content-Security-Policy"] = csp;
    headers["X-Frame-Options"] = "DENY";
    headers["X-Content-Type-Options"] = "nosniff";
    headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=(), usb=(), interest-cohort=()";

    await next();
});

app.UseDefaultFiles();
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = context =>
    {
        context.Context.Response.Headers.CacheControl = "no-store, no-cache, max-age=0";
        context.Context.Response.Headers.Pragma = "no-cache";
    }
});

app.MapGet("/health", () => Results.Ok(new
{
    service = "SpareParts.Web.React",
    status = "ok",
    generatedAt = DateTime.UtcNow
}));

app.MapFallbackToFile("index.html");

app.Run();
