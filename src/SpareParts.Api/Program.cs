using SpareParts.Infrastructure.Data;
using SpareParts.Infrastructure.Services;
using SpareParts.Domain.Sales;
using SpareParts.Domain.Purchases;

var builder = WebApplication.CreateBuilder(args);

// Config
var connString = builder.Configuration.GetConnectionString("DefaultConnection") 
                 ?? "Server=localhost;Database=SparePartsDb;Trusted_Connection=True;TrustServerCertificate=True;";

// DI
builder.Services.AddSingleton<ISqlConnectionFactory>(_ => new SqlConnectionFactory(connString));
builder.Services.AddScoped<IAccountingStrategy<SalesInvoice>>(_ =>
    new SaleAccountingStrategy(
        cashAccountId: 1,
        salesAccountId: 5,
        cogsAccountId: 6,
        inventoryAccountId: 2));
builder.Services.AddScoped<IAccountingStrategy<PurchaseInvoice>>(_ =>
    new PurchaseAccountingStrategy(
        inventoryAccountId: 2,
        cashOrApAccountId: 4)); // Accounts Payable

builder.Services.AddScoped<SalesService>();
builder.Services.AddScoped<PurchaseService>();

builder.Services.AddControllers();

var app = builder.Build();

app.MapControllers();

app.Run();
