using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using SpareParts.Api.Controllers;
using SpareParts.Api.Errors;
using SpareParts.Api.Infrastructure;
using SpareParts.Api.Services;
using SpareParts.Domain.Purchases;
using SpareParts.Domain.Sales;
using SpareParts.Infrastructure.Data;
using SpareParts.Infrastructure.Services;
using System.Net.Http.Headers;
using System.Text;

namespace SpareParts.Api.Hosting;

public static class SparePartsApiComposition
{
    private static readonly Dictionary<ServiceCapability, string[]> ControllerMap = new()
    {
        [ServiceCapability.Sales] = [nameof(SalesController), nameof(CustomersController)],
        [ServiceCapability.Purchases] = [nameof(PurchasesController), nameof(SuppliersController)],
        [ServiceCapability.Inventory] = [nameof(PartsController), nameof(WarehousesController), nameof(TransactionTypesController)],
        [ServiceCapability.Accounting] = [nameof(AccountsController), nameof(AccountingController)],
        [ServiceCapability.Identity] = [nameof(AuthController), nameof(UsersController), nameof(RolesController)],
        [ServiceCapability.Catalog] = [nameof(BrandsController), nameof(CategoriesController), nameof(CarBrandsController), nameof(CarModelsController), nameof(LocationsController), nameof(UsedCarsController), nameof(CurrenciesController), nameof(AppConstantsController)],
        [ServiceCapability.Health] = [nameof(HealthController)]
    };

    static SparePartsApiComposition()
    {
        var unmappedCapabilities = Enum.GetValues<ServiceCapability>()
            .Except(ControllerMap.Keys)
            .ToArray();

        if (unmappedCapabilities.Length > 0)
        {
            throw new InvalidOperationException($"Missing controller mappings for capabilities: {string.Join(", ", unmappedCapabilities)}");
        }
    }

    public static void AddSparePartsApiCore(this WebApplicationBuilder builder)
    {
        AccountingDapperBootstrap.EnsureConfigured();

        var connString = ResolveConnectionString(builder);
        var jwtSettings = ResolveJwtSettings(builder.Configuration);
        var accountingOptions = builder.Configuration.GetSection("Accounting").Get<AccountingOptions>() ?? new AccountingOptions();
        var openAiOptions = ResolveOpenAiOptions(builder.Configuration);

        builder.Services.AddSingleton(accountingOptions);
        builder.Services.AddSingleton(openAiOptions);

        builder.Services.AddSingleton<ISqlConnectionFactory>(_ => new SqlConnectionFactory(connString));
        builder.Services.AddSingleton<IExceptionLogWriter, SqlExceptionLogWriter>();
        builder.Services.AddSingleton(jwtSettings);
        builder.Services.AddSingleton<AccountingSettingsProvider>();

        builder.Services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(opt =>
            {
                opt.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
                    ValidateIssuer = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwtSettings.Audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };
            });

        builder.Services.AddAuthorization();

        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
        var isDevelopment = builder.Environment.IsDevelopment();

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
    }

    public static void AddCapabilities(this IServiceCollection services, string serviceName, params ServiceCapability[] capabilities)
    {
        var distinctCapabilities = capabilities.Distinct().ToArray();

        services.AddSingleton(new ServiceProfile(serviceName, distinctCapabilities));

        if (distinctCapabilities.Contains(ServiceCapability.Sales))
        {
            services.AddScoped<CustomerAccountResolver>();
            services.AddScoped<IAccountingStrategy<SalesInvoice>>(sp =>
            {
                return new SaleAccountingStrategy(
                    sp.GetRequiredService<AccountingSettingsProvider>(),
                    sp.GetRequiredService<CustomerAccountResolver>());
            });

            services.AddScoped<ICreateSaleHandler, CreateSaleHandler>();
            services.AddScoped<SalesService>();
            RegisterSharedInvoiceServices(services);
            services.AddScoped<CustomersService>();
        }

        if (distinctCapabilities.Contains(ServiceCapability.Purchases))
        {
            services.AddScoped<SupplierAccountResolver>();
            services.AddScoped<IAccountingStrategy<PurchaseInvoice>>(sp =>
            {
                return new PurchaseAccountingStrategy(
                    sp.GetRequiredService<AccountingSettingsProvider>(),
                    sp.GetRequiredService<SupplierAccountResolver>());
            });

            services.AddScoped<ICreatePurchaseHandler, CreatePurchaseHandler>();
            services.AddScoped<ICreateUsedCarPurchaseHandler, CreateUsedCarPurchaseHandler>();
            services.AddScoped<PurchaseService>();
            RegisterSharedInvoiceServices(services);
            services.AddScoped<SuppliersService>();
        }

        if (distinctCapabilities.Contains(ServiceCapability.Inventory))
        {
            services.AddHttpClient<PartNotesAiService>((serviceProvider, client) =>
            {
                var options = serviceProvider.GetRequiredService<OpenAiOptions>();
                client.BaseAddress = new Uri(options.BaseUrl, UriKind.Absolute);
                client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            });

            services.AddScoped<PartsService>();
            services.AddScoped<WarehousesService>();
            services.AddScoped<TransactionTypesService>();
        }

        if (distinctCapabilities.Contains(ServiceCapability.Accounting))
        {
            services.AddScoped<AccountingService>();
            services.AddScoped<CustomerAccountResolver>();
            services.AddScoped<SupplierAccountResolver>();
        }

        if (distinctCapabilities.Contains(ServiceCapability.Identity))
        {
            services.AddScoped<AuthService>();
            services.AddScoped<UsersService>();
            services.AddScoped<RolesService>();
        }

        if (distinctCapabilities.Contains(ServiceCapability.Catalog))
        {
            services.AddScoped<BrandsService>();
            services.AddScoped<CategoriesService>();
            services.AddScoped<CarBrandsService>();
            services.AddScoped<CarModelsService>();
            services.AddScoped<LocationsService>();
            services.AddScoped<UsedCarsService>();
            services.AddScoped<UsedCarImagesService>();
            services.AddScoped<CurrenciesService>();
            services.AddScoped<AppConstantsService>();
        }
    }

    public static IMvcBuilder AddCapabilityControllers(this IServiceCollection services, params ServiceCapability[] capabilities)
    {
        var distinctCapabilities = capabilities.Distinct().ToArray();

        var allowedControllers = distinctCapabilities
            .SelectMany(capability => ControllerMap[capability])
            .ToHashSet(StringComparer.Ordinal);

        return services
            .AddControllers()
            .ConfigureApplicationPartManager(manager =>
            {
                manager.FeatureProviders.Add(new CapabilityControllerFeatureProvider(allowedControllers));
            });
    }

    private static void RegisterSharedInvoiceServices(IServiceCollection services)
    {
        services.TryAddSingleton<IInvoiceNumberGenerator, UtcInvoiceNumberGenerator>();
        services.TryAddSingleton<IPaymentStatusPolicy, DefaultPaymentStatusPolicy>();
        services.TryAddSingleton<IInvoiceTotalsCalculator, InvoiceTotalsCalculator>();
        services.TryAddScoped<IInventoryService, InventoryService>();
    }

    public static void UseSparePartsApiPipeline(this WebApplication app)
    {
        var sqlConnectionFactory = app.Services.GetRequiredService<ISqlConnectionFactory>();
        InvoiceNumberingMigration.EnsureApplied(sqlConnectionFactory);
        AccountingMigration.EnsureApplied(sqlConnectionFactory);
        MenuAccessMigration.EnsureApplied(sqlConnectionFactory);
        TransactionTypesMigration.EnsureApplied(sqlConnectionFactory);
        PartAveragePriceMigration.EnsureApplied(sqlConnectionFactory);
        PartUsedCarMigration.EnsureApplied(sqlConnectionFactory);
        CurrencyRatesMigration.EnsureApplied(sqlConnectionFactory);
        AppConstantsMigration.EnsureApplied(sqlConnectionFactory);
        CarModelsMigration.EnsureApplied(sqlConnectionFactory);
        LocationsMigration.EnsureApplied(sqlConnectionFactory);
        UsedCarsMigration.EnsureApplied(sqlConnectionFactory);
        UsedCarPurchasesMigration.EnsureApplied(sqlConnectionFactory);
        TransactionsMigration.EnsureApplied(sqlConnectionFactory);
        UsedCarImagesMigration.EnsureApplied(sqlConnectionFactory);

        app.UseMiddleware<ApiExceptionMiddleware>();
        app.UseCors();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
    }

    private static string ResolveConnectionString(WebApplicationBuilder builder)
    {
        var connString = builder.Configuration.GetConnectionString("DefaultConnection");
        if (!string.IsNullOrWhiteSpace(connString))
        {
            return connString;
        }

        if (builder.Environment.IsDevelopment())
        {
            return "Server=localhost;Database=SparePartsDb;Trusted_Connection=True;TrustServerCertificate=True;";
        }

        throw new InvalidOperationException("Missing required connection string: ConnectionStrings:DefaultConnection");
    }

    private static JwtSettings ResolveJwtSettings(IConfiguration configuration)
    {
        var jwtSection = configuration.GetSection("Jwt");
        var jwtSecret = jwtSection["Secret"];

        if (string.IsNullOrWhiteSpace(jwtSecret))
        {
            throw new InvalidOperationException("Missing required JWT secret: Jwt:Secret");
        }

        return new JwtSettings
        {
            Secret = jwtSecret,
            Issuer = jwtSection["Issuer"] ?? "SpareParts.Api",
            Audience = jwtSection["Audience"] ?? "SpareParts.Desktop",
            ExpiryHours = int.TryParse(jwtSection["ExpiryHours"], out var hours) ? hours : 12
        };
    }

    private static OpenAiOptions ResolveOpenAiOptions(IConfiguration configuration)
    {
        var section = configuration.GetSection("OpenAI");
        var apiKey = section["ApiKey"] ?? configuration["OPENAI_API_KEY"];
        if (!string.IsNullOrWhiteSpace(apiKey) && apiKey.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            apiKey = apiKey["Bearer ".Length..];
        }

        var baseUrl = section["BaseUrl"];
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            baseUrl = "https://api.openai.com/v1/";
        }

        baseUrl = baseUrl.Trim();
        if (!baseUrl.EndsWith("/", StringComparison.Ordinal))
        {
            baseUrl = $"{baseUrl}/";
        }

        var timeoutSeconds = int.TryParse(section["TimeoutSeconds"], out var configuredTimeout)
            ? Math.Clamp(configuredTimeout, 5, 120)
            : 30;

        return new OpenAiOptions
        {
            ApiKey = string.IsNullOrWhiteSpace(apiKey) ? null : apiKey.Trim(),
            BaseUrl = baseUrl,
            Model = string.IsNullOrWhiteSpace(section["Model"]) ? "gpt-5-mini" : section["Model"]!.Trim(),
            TimeoutSeconds = timeoutSeconds
        };
    }

}
