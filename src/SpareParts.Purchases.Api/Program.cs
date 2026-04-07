using SpareParts.Api.Hosting;

var builder = WebApplication.CreateBuilder(args);
builder.AddSparePartsApiCore();
builder.Services.AddCapabilities(builder.Environment.ApplicationName, ServiceCapability.Purchases, ServiceCapability.Health);
builder.Services.AddCapabilityControllers(ServiceCapability.Purchases, ServiceCapability.Health);

var app = builder.Build();
app.UseSparePartsApiPipeline();
app.Run();
