using SpareParts.Api.Hosting;

var builder = WebApplication.CreateBuilder(args);

builder.AddSparePartsApiCore();
builder.Services.AddCapabilities(builder.Environment.ApplicationName, 
    ServiceCapability.Sales,
    ServiceCapability.Purchases,
    ServiceCapability.Inventory,
    ServiceCapability.Accounting,
    ServiceCapability.Identity,
    ServiceCapability.Catalog,
    ServiceCapability.Health);
builder.Services.AddCapabilityControllers(
    ServiceCapability.Sales,
    ServiceCapability.Purchases,
    ServiceCapability.Inventory,
    ServiceCapability.Accounting,
    ServiceCapability.Identity,
    ServiceCapability.Catalog,
    ServiceCapability.Health);

var app = builder.Build();
app.UseSparePartsApiPipeline();
app.Run();

public class JwtSettings
{
    public string Secret { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int ExpiryHours { get; set; } = 12;
}
