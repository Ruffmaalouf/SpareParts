var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

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
