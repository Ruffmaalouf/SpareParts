using SpareParts.Api.Hosting;

var builder = WebApplication.CreateBuilder(args);
builder.AddSparePartsApiCore();
builder.Services.AddCapabilities(builder.Environment.ApplicationName, ServiceCapability.Sales, ServiceCapability.Health);
builder.Services.AddCapabilityControllers(ServiceCapability.Sales, ServiceCapability.Health);

var app = builder.Build();
app.UseSparePartsApiPipeline();
app.Run();
