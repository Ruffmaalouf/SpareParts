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
    ServiceCapability.Reporting,
    ServiceCapability.Health,
    ServiceCapability.Billing);
builder.Services.AddCapabilityControllers(
    ServiceCapability.Sales,
    ServiceCapability.Purchases,
    ServiceCapability.Inventory,
    ServiceCapability.Accounting,
    ServiceCapability.Identity,
    ServiceCapability.Catalog,
    ServiceCapability.Reporting,
    ServiceCapability.Health,
    ServiceCapability.Billing);

var app = builder.Build();
app.UseSparePartsApiPipeline();
app.Run();
