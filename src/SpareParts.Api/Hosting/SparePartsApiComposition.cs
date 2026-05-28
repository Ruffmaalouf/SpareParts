using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using SpareParts.Api.Controllers;
using SpareParts.Api.Errors;
using SpareParts.Api.Infrastructure;
using SpareParts.Api.Middleware;
using SpareParts.Api.Notifications;
using SpareParts.Api.Services;
using SpareParts.Domain.Purchases;
using SpareParts.Domain.Sales;
using SpareParts.Infrastructure.Data;
using SpareParts.Infrastructure.Interfaces;
using SpareParts.Infrastructure.Services;
using System.Net.Http.Headers;
using System.Text;

namespace SpareParts.Api.Hosting;

public static class SparePartsApiComposition
{
    public const string NotificationsHubPath = "/hubs/notifications";

    public static readonly IReadOnlyList<ServiceProfile> ExpectedServiceProfiles =
    [
        new("SpareParts.Api",
        [
            ServiceCapability.Sales,
            ServiceCapability.Purchases,
            ServiceCapability.Inventory,
            ServiceCapability.Accounting,
            ServiceCapability.Identity,
            ServiceCapability.Catalog,
            ServiceCapability.Reporting,
            ServiceCapability.Health
        ]),
        new("SpareParts.Sales.Api", [ServiceCapability.Sales, ServiceCapability.Health]),
        new("SpareParts.Purchases.Api", [ServiceCapability.Purchases, ServiceCapability.Health]),
        new("SpareParts.Inventory.Api", [ServiceCapability.Inventory, ServiceCapability.Health]),
        new("SpareParts.Identity.Api", [ServiceCapability.Identity, ServiceCapability.Health]),
        new("SpareParts.Catalog.Api", [ServiceCapability.Catalog, ServiceCapability.Health])
    ];

    public static readonly IReadOnlyList<string> MigrationNames =
    [
        nameof(InvoiceNumberingMigration),
        nameof(AccountingMigration),
        nameof(WebAppUserRoleMigration),
        nameof(UserRoleIdMigration),
        nameof(MenuAccessMigration),
        nameof(TransactionTypesMigration),
        nameof(PartAveragePriceMigration),
        nameof(PartUsedCarMigration),
        nameof(CurrencyRatesMigration),
        nameof(AppConstantsMigration),
        nameof(CarModelsMigration),
        nameof(LocationsMigration),
        nameof(UsedCarsMigration),
        nameof(UsedCarPurchasesMigration),
        nameof(UsedCarWholesaleSalesMigration),
        nameof(TransactionsMigration),
        nameof(BarcodeScanningMigration),
        nameof(PartRequestsMigration),
        nameof(PartUsedCarStockMigration),
        nameof(UsedCarImagesMigration),
        nameof(CommunicationsMigration),
        nameof(WhatsAppCampaignsMigration),
        nameof(ReportBuilderLinksMigration),
        nameof(ReportBuilderAdvancedMigration)
    ];

    private static readonly Dictionary<ServiceCapability, string[]> ControllerMap = new()
    {
        [ServiceCapability.Sales] = [nameof(SalesController), nameof(CustomersController), nameof(WebCatalogController)],
        [ServiceCapability.Purchases] = [nameof(PurchasesController), nameof(SuppliersController)],
        [ServiceCapability.Inventory] = [nameof(PartsController), nameof(PartRequestsController), nameof(WarehousesController), nameof(TransactionTypesController), nameof(ScansController)],
        [ServiceCapability.Accounting] = [nameof(AccountsController), nameof(AccountingController)],
        [ServiceCapability.Identity] = [nameof(AuthController), nameof(UsersController), nameof(RolesController)],
        [ServiceCapability.Catalog] = [nameof(BrandsController), nameof(CategoriesController), nameof(CarBrandsController), nameof(CarModelsController), nameof(LocationsController), nameof(UsedCarsController), nameof(CurrenciesController), nameof(AppConstantsController), nameof(ExcelImportController)],
        [ServiceCapability.Reporting] = [nameof(ReportBuilderController), nameof(OwnerCockpitController), nameof(BusinessAssistantController), nameof(CommunicationsController), nameof(SearchController)],
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
        var communicationOptions = ResolveCommunicationOptions(builder.Configuration);
        var externalAuthSettings = builder.Configuration.GetSection("ExternalAuth").Get<ExternalAuthSettings>() ?? new ExternalAuthSettings();

        builder.Services.AddSingleton(accountingOptions);
        builder.Services.AddSingleton(openAiOptions);
        builder.Services.AddSingleton(communicationOptions);
        builder.Services.AddSingleton(externalAuthSettings);

        builder.Services.AddSingleton<ISqlConnectionFactory>(_ => new SqlConnectionFactory(connString));
        builder.Services.AddSingleton<IExceptionLogWriter, SqlExceptionLogWriter>();
        builder.Services.AddSingleton(jwtSettings);
        builder.Services.AddSingleton<AccountingSettingsProvider>();

        if (communicationOptions.HasWebhook)
        {
            builder.Services
                .AddHttpClient<ICommunicationDeliveryClient, WebhookCommunicationDeliveryClient>(client =>
                {
                    client.Timeout = TimeSpan.FromSeconds(communicationOptions.TimeoutSeconds);
                });
        }
        else
        {
            builder.Services.AddSingleton<ICommunicationDeliveryClient, DisabledCommunicationDeliveryClient>();
        }

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
                opt.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"].ToString();
                        var path = context.HttpContext.Request.Path;

                        if (!string.IsNullOrWhiteSpace(accessToken) &&
                            path.StartsWithSegments("/hubs/notifications"))
                        {
                            context.Token = accessToken;
                        }

                        return Task.CompletedTask;
                    }
                };
            });

        builder.Services.AddAuthorization(AuthorizationPolicies.AddRoleIdPolicies);
        builder.Services.AddSignalR();

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
                    sp.GetRequiredService<ISqlConnectionFactory>(),
                    sp.GetRequiredService<AccountingSettingsProvider>(),
                    sp.GetRequiredService<CustomerAccountResolver>());
            });

            services.AddScoped<ICreateSaleHandler, CreateSaleHandler>();
            services.AddScoped<SalesService>();
            services.AddScoped<WebCatalogService>();
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
            services.AddHttpClient<VisualPartSearchService>((serviceProvider, client) =>
            {
                var options = serviceProvider.GetRequiredService<OpenAiOptions>();
                client.BaseAddress = new Uri(options.BaseUrl, UriKind.Absolute);
                client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            });

            services.AddScoped<PartsService>();
            services.AddScoped<PartRequestsService>();
            services.AddHostedService<PartReservationClockHostedService>();
            services.AddScoped<WarehousesService>();
            services.AddScoped<TransactionTypesService>();
            services.AddScoped<ScanLookupService>();
        }

        if (distinctCapabilities.Contains(ServiceCapability.Accounting))
        {
            services.AddScoped<AccountingService>();
            services.AddScoped<CustomerAccountResolver>();
            services.AddScoped<SupplierAccountResolver>();
        }

        if (distinctCapabilities.Contains(ServiceCapability.Identity))
        {
            services.AddHttpClient<AuthService>();
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
            services.AddScoped<ExcelImportService>();
        }

        if (distinctCapabilities.Contains(ServiceCapability.Reporting))
        {
            services.TryAddScoped<AccountingService>();
            services.TryAddScoped<ScanLookupService>();
            services.AddScoped<BusinessAssistantService>();
            services.AddScoped<CommunicationsService>();
            services.AddScoped<WhatsAppCampaignService>();
            services.AddScoped<ReportBuilderService>();
            services.AddScoped<OwnerCockpitService>();
            services.AddScoped<SmartSearchService>();
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
        WebAppUserRoleMigration.EnsureApplied(sqlConnectionFactory);
        UserRoleIdMigration.EnsureApplied(sqlConnectionFactory);
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
        UsedCarWholesaleSalesMigration.EnsureApplied(sqlConnectionFactory);
        TransactionsMigration.EnsureApplied(sqlConnectionFactory);
        AccountingCurrencyRateRepairMigration.EnsureApplied(sqlConnectionFactory);
        BarcodeScanningMigration.EnsureApplied(sqlConnectionFactory);
        PartRequestsMigration.EnsureApplied(sqlConnectionFactory);
        PartUsedCarStockMigration.EnsureApplied(sqlConnectionFactory);
        UsedCarImagesMigration.EnsureApplied(sqlConnectionFactory);
        CommunicationsMigration.EnsureApplied(sqlConnectionFactory);
        WhatsAppCampaignsMigration.EnsureApplied(sqlConnectionFactory);
        ReportBuilderLinksMigration.EnsureApplied(sqlConnectionFactory);
        ReportBuilderAdvancedMigration.EnsureApplied(sqlConnectionFactory);

        app.UseMiddleware<ApiExceptionMiddleware>();
        app.UseCors();
        app.UseAuthentication();
        app.UseMiddleware<WebAppUserRestrictionMiddleware>();
        app.UseAuthorization();
        app.MapHub<NotificationsHub>(NotificationsHubPath);
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

    private static CommunicationOptions ResolveCommunicationOptions(IConfiguration configuration)
    {
        var section = configuration.GetSection("Communications");
        var timeoutSeconds = int.TryParse(section["TimeoutSeconds"], out var configuredTimeout)
            ? Math.Clamp(configuredTimeout, 3, 120)
            : 15;

        return new CommunicationOptions
        {
            Provider = string.IsNullOrWhiteSpace(section["Provider"]) ? "Webhook" : section["Provider"]!.Trim(),
            WebhookUrl = string.IsNullOrWhiteSpace(section["WebhookUrl"]) ? null : section["WebhookUrl"]!.Trim(),
            WebhookSecret = string.IsNullOrWhiteSpace(section["WebhookSecret"]) ? null : section["WebhookSecret"]!.Trim(),
            TimeoutSeconds = timeoutSeconds
        };
    }

}
