using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using SpareParts.Infrastructure.Data;
using SpareParts.Infrastructure.Services;
using SpareParts.Domain.Sales;
using SpareParts.Domain.Purchases;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ── Connection string ─────────────────────────────────────────────────────────
var connString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Server=localhost;Database=SparePartsDb;Trusted_Connection=True;TrustServerCertificate=True;";

// ── JWT settings ──────────────────────────────────────────────────────────────
var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtSecret  = jwtSection["Secret"]
    ?? "MaaloufAutoPartsSecretKey_ChangeInProduction_2024!";
var jwtIssuer   = jwtSection["Issuer"]   ?? "SpareParts.Api";
var jwtAudience = jwtSection["Audience"] ?? "SpareParts.Desktop";
var jwtExpHours = int.TryParse(jwtSection["ExpiryHours"], out var h) ? h : 12;

// ── DI ────────────────────────────────────────────────────────────────────────
builder.Services.AddSingleton<ISqlConnectionFactory>(_ =>
    new SqlConnectionFactory(connString));

builder.Services.AddSingleton(new JwtSettings
{
    Secret     = jwtSecret,
    Issuer     = jwtIssuer,
    Audience   = jwtAudience,
    ExpiryHours = jwtExpHours
});

builder.Services.AddScoped<IAccountingStrategy<SalesInvoice>>(_ =>
    new SaleAccountingStrategy(
        cashAccountId:      1,
        salesAccountId:     5,
        cogsAccountId:      6,
        inventoryAccountId: 2));

builder.Services.AddScoped<IAccountingStrategy<PurchaseInvoice>>(_ =>
    new PurchaseAccountingStrategy(
        inventoryAccountId:  2,
        cashOrApAccountId:   4));

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
            IssuerSigningKey         = new SymmetricSecurityKey(
                                           Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuer   = true,
            ValidIssuer      = jwtIssuer,
            ValidateAudience = true,
            ValidAudience    = jwtAudience,
            ValidateLifetime = true,
            ClockSkew        = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddControllers();

// ── CORS (allow WPF desktop loopback) ────────────────────────────────────────
builder.Services.AddCors(opt =>
    opt.AddDefaultPolicy(p =>
        p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

var app = builder.Build();

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

// ── Helper class (used by AuthController) ────────────────────────────────────
public class JwtSettings
{
    public string Secret      { get; set; } = string.Empty;
    public string Issuer      { get; set; } = string.Empty;
    public string Audience    { get; set; } = string.Empty;
    public int    ExpiryHours { get; set; } = 12;
}
