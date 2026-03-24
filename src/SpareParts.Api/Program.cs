using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SpareParts.Domain.Purchases;
using SpareParts.Domain.Sales;
using SpareParts.Infrastructure.Data;
using SpareParts.Infrastructure.Services;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
var isDevelopment = builder.Environment.IsDevelopment();

// ── Connection string ─────────────────────────────────────────────────────────
var connString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connString))
{
    if (isDevelopment)
    {
        connString = "Server=localhost;Database=SparePartsDb;Trusted_Connection=True;TrustServerCertificate=True;";
    }
    else
    {
        throw new InvalidOperationException("Missing required connection string: ConnectionStrings:DefaultConnection");
    }
}

// ── JWT settings ──────────────────────────────────────────────────────────────
var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtSecret = jwtSection["Secret"];
if (string.IsNullOrWhiteSpace(jwtSecret))
{
    throw new InvalidOperationException("Missing required JWT secret: Jwt:Secret");
}
var jwtIssuer = jwtSection["Issuer"] ?? "SpareParts.Api";
var jwtAudience = jwtSection["Audience"] ?? "SpareParts.Desktop";
var jwtExpHours = int.TryParse(jwtSection["ExpiryHours"], out var h) ? h : 12;

builder.Services.Configure<AccountingOptions>(builder.Configuration.GetSection("Accounting"));

// ── DI ────────────────────────────────────────────────────────────────────────
builder.Services.AddSingleton<ISqlConnectionFactory>(_ =>
    new SqlConnectionFactory(connString));

builder.Services.AddSingleton(new JwtSettings
{
    Secret = jwtSecret,
    Issuer = jwtIssuer,
    Audience = jwtAudience,
    ExpiryHours = jwtExpHours
});

builder.Services.AddScoped<IAccountingStrategy<SalesInvoice>>(sp =>
{
    var options = sp.GetRequiredService<IOptions<AccountingOptions>>().Value;
    return new SaleAccountingStrategy(
        cashAccountId: options.CashAccountId,
        salesAccountId: options.SalesAccountId,
        cogsAccountId: options.CogsAccountId,
        inventoryAccountId: options.InventoryAccountId);
});

builder.Services.AddScoped<IAccountingStrategy<PurchaseInvoice>>(sp =>
{
    var options = sp.GetRequiredService<IOptions<AccountingOptions>>().Value;
    return new PurchaseAccountingStrategy(
        inventoryAccountId: options.InventoryAccountId,
        cashOrApAccountId: options.CashOrApAccountId);
});

builder.Services.AddScoped<ISparePartsDataContextFactory, SparePartsDataContextFactory>();
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddSingleton<IInvoiceNumberGenerator, UtcInvoiceNumberGenerator>();
builder.Services.AddSingleton<IPaymentStatusPolicy, DefaultPaymentStatusPolicy>();

builder.Services.AddScoped<SalesService>();
builder.Services.AddScoped<PurchaseService>();

// ── JWT Authentication ────────────────────────────────────────────────────────
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddControllers();

// ── CORS (allow WPF desktop loopback) ────────────────────────────────────────
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
builder.Services.AddCors(opt =>
    opt.AddDefaultPolicy(p =>
    {
        if (allowedOrigins is { Length: > 0 })
            p.WithOrigins(allowedOrigins).AllowAnyMethod().AllowAnyHeader();
        else if (isDevelopment)
            p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
        else
            p.WithOrigins("http://localhost:5000").AllowAnyMethod().AllowAnyHeader();
    }));

var app = builder.Build();

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

// ── Helper class (used by AuthController) ────────────────────────────────────
public class JwtSettings
{
    public string Secret { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int ExpiryHours { get; set; } = 12;
}
