using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
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
using SpareParts.Infrastructure.Services.Pricing;
using ITenantContext = SpareParts.Infrastructure.Interfaces.ITenantContext;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.RateLimiting;

namespace SpareParts.Api.Hosting;

public static class SparePartsApiComposition
{
    public const string NotificationsHubPath = "/hubs/notifications";
    public const string AuthRateLimitPolicy = "auth-login";

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
            ServiceCapability.Health,
            ServiceCapability.Billing
        ]),
        new("SpareParts.Sales.Api", [ServiceCapability.Sales, ServiceCapability.Health]),
        new("SpareParts.Purchases.Api", [ServiceCapability.Purchases, ServiceCapability.Health]),
        new("SpareParts.Inventory.Api", [ServiceCapability.Inventory, ServiceCapability.Health]),
        new("SpareParts.Identity.Api", [ServiceCapability.Identity, ServiceCapability.Health]),
        new("SpareParts.Catalog.Api", [ServiceCapability.Catalog, ServiceCapability.Health])
    ];

    public static readonly IReadOnlyList<string> MigrationNames =
    [
        nameof(TenantsMigration),
        nameof(TenantIdMigration),
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
        nameof(UsedCarPartPricingMigration),
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
        nameof(ReportBuilderAdvancedMigration),
        nameof(ReorderRulesMigration),
        nameof(PartSubstitutesMigration),
        nameof(PartExpiryMigration),
        nameof(CustomerLoyaltyMigration),
        nameof(CustomerPriceTierMigration),
        nameof(WarrantyClaimsMigration),
        nameof(SupplierPriceHistoryMigration),
        nameof(ShipmentsMigration),
        nameof(ActivityLogMigration),
        nameof(QuotesMigration),
        nameof(CustomerCreditLimitMigration),
        nameof(PricingPackagesMigration),
        nameof(UserVehiclesMigration),
        nameof(NeedBoardMigration),
        nameof(WatchlistMigration),
        nameof(SellerVerificationMigration),
        nameof(MarketplaceFeaturesMigration)
    ];

    private static readonly Dictionary<ServiceCapability, string[]> ControllerMap = new()
    {
        [ServiceCapability.Sales] = [nameof(SalesController), nameof(CustomersController), nameof(WebCatalogController), nameof(LoyaltyController), nameof(CustomerPricingController), nameof(WarrantyController), nameof(ShipmentsController), nameof(QuotesController)],
        [ServiceCapability.Purchases] = [nameof(PurchasesController), nameof(SuppliersController), nameof(SupplierPriceHistoryController)],
        [ServiceCapability.Inventory] = [nameof(PartsController), nameof(PartRequestsController), nameof(WarehousesController), nameof(TransactionTypesController), nameof(ScansController), nameof(ReorderController), nameof(PartSubstitutesController), nameof(PartExpiryController)],
        [ServiceCapability.Accounting] = [nameof(AccountsController), nameof(AccountingController)],
        [ServiceCapability.Identity] = [nameof(AuthController), nameof(UsersController), nameof(RolesController), nameof(TenantsController)],
        [ServiceCapability.Catalog] = [nameof(BrandsController), nameof(CategoriesController), nameof(CarBrandsController), nameof(CarModelsController), nameof(LocationsController), nameof(UsedCarsController), nameof(CurrenciesController), nameof(AppConstantsController), nameof(ExcelImportController)],
        [ServiceCapability.Reporting] = [nameof(ReportBuilderController), nameof(OwnerCockpitController), nameof(BusinessAssistantController), nameof(CommunicationsController), nameof(SearchController), nameof(GrowthController), nameof(ActivityLogController)],
        [ServiceCapability.Health] = [nameof(HealthController)],
        [ServiceCapability.Billing] = [nameof(PricingController), nameof(SubscriptionController), nameof(PaymentsController), nameof(InvoicesController), nameof(AdminPricingController), nameof(AdminSubscriptionsController), nameof(AdminPaymentsController), nameof(AdminInvoicesController)]
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
        var paymentSettings = builder.Configuration.GetSection("Payments").Get<PaymentSettings>() ?? new PaymentSettings();

        builder.Services.AddSingleton(accountingOptions);
        builder.Services.AddSingleton(openAiOptions);
        builder.Services.AddSingleton(communicationOptions);
        builder.Services.AddSingleton(externalAuthSettings);
        builder.Services.AddSingleton(paymentSettings);

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

        builder.Services.AddRateLimiter(opt =>
        {
            opt.OnRejected = async (ctx, _) =>
            {
                ctx.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                await ctx.HttpContext.Response.WriteAsync("Too many requests. Please try again later.");
            };

            opt.AddPolicy(AuthRateLimitPolicy, httpCtx =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpCtx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 10,
                        Window = TimeSpan.FromMinutes(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    }));
        });
    }

    public static void AddCapabilities(this IServiceCollection services, string serviceName, params ServiceCapability[] capabilities)
    {
        var distinctCapabilities = capabilities.Distinct().ToArray();

        services.AddSingleton(new ServiceProfile(serviceName, distinctCapabilities));

        // Tenant context is always registered — scoped per HTTP request, populated by TenantResolutionMiddleware.
        services.AddScoped<TenantContext>();
        services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<TenantContext>());
        services.AddScoped<TenantsService>();

        // Pricing/subscription/payment services are always registered — ISubscriptionLimitService is consulted
        // by feature/limit checks across other capabilities (Inventory, Identity, Sales, ...).
        services.AddScoped<IPricingPackageService, PricingPackageService>();
        services.AddScoped<ISubscriptionLimitService, SubscriptionLimitService>();
        services.AddScoped<ISubscriptionService, SubscriptionService>();
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<IInvoiceService, InvoiceService>();
        services.AddScoped<IPaymentProviderFactory, PaymentProviderFactory>();
        services.AddScoped<IPaymentProvider, ManualPaymentProvider>();
        services.AddScoped<IPaymentProvider, TestPaymentProvider>();
        services.AddHttpClient<IPaymentProvider, StripePaymentProvider>();
        services.AddHostedService<SubscriptionMaintenanceHostedService>();

        if (distinctCapabilities.Contains(ServiceCapability.Sales))
        {
            services.AddScoped<LoyaltyService>();
            services.AddScoped<CustomerPriceTierService>();
            services.AddScoped<WarrantyService>();
            services.AddScoped<ShipmentsService>();
            services.AddScoped<CustomerAccountResolver>();
            services.AddScoped<IAccountingStrategy<SalesInvoice>>(sp =>
            {
                return new SaleAccountingStrategy(
                    sp.GetRequiredService<ISqlConnectionFactory>(),
                    sp.GetRequiredService<AccountingSettingsProvider>(),
                    sp.GetRequiredService<CustomerAccountResolver>(),
                    sp.GetService<ILogger<SaleAccountingStrategy>>());
            });

            services.AddScoped<ICreateSaleHandler, CreateSaleHandler>();
            services.AddScoped<SalesService>();
            services.AddScoped<QuotesService>();
            services.AddScoped<WebCatalogService>();
            services.TryAddScoped<PartRequestsService>();
            RegisterVisualPartSearchService(services);
            RegisterSharedInvoiceServices(services);
            services.AddScoped<CustomersService>();
            services.AddHostedService<QuoteExpiryHostedService>();
        }

        if (distinctCapabilities.Contains(ServiceCapability.Purchases))
        {
            services.AddScoped<SupplierPriceHistoryService>();
            services.AddScoped<SupplierAccountResolver>();
            services.AddScoped<IAccountingStrategy<PurchaseInvoice>>(sp =>
            {
                return new PurchaseAccountingStrategy(
                    sp.GetRequiredService<AccountingSettingsProvider>(),
                    sp.GetRequiredService<SupplierAccountResolver>(),
                    sp.GetService<ILogger<PurchaseAccountingStrategy>>());
            });

            services.AddScoped<ICreatePurchaseHandler, CreatePurchaseHandler>();
            services.AddScoped<ICreateUsedCarPurchaseHandler, CreateUsedCarPurchaseHandler>();
            services.AddScoped<PurchaseService>();
            RegisterSharedInvoiceServices(services);
            services.AddScoped<SuppliersService>();
        }

        if (distinctCapabilities.Contains(ServiceCapability.Inventory))
        {
            services.TryAddScoped<IInventoryService, InventoryService>();

            services.AddScoped<ReorderAnalysisService>();
            services.AddScoped<PartSubstitutesService>();
            services.AddScoped<PartExpiryService>();
            services.AddHttpClient<PartNotesAiService>((serviceProvider, client) =>
            {
                var options = serviceProvider.GetRequiredService<OpenAiOptions>();
                client.BaseAddress = new Uri(options.BaseUrl, UriKind.Absolute);
                client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            });
            RegisterVisualPartSearchService(services);

            services.AddScoped<PartsService>();
            services.AddScoped<PartRequestsService>();
            services.AddHostedService<PartReservationClockHostedService>();
            services.AddHostedService<ReservationExpiryHostedService>();
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
            services.AddScoped<ActivityLogService>();
            services.TryAddScoped<AccountingService>();
            services.TryAddScoped<ScanLookupService>();
            services.AddScoped<BusinessAssistantService>();
            services.AddScoped<CommunicationsService>();
            services.AddScoped<WhatsAppCampaignService>();
            services.AddScoped<ReportBuilderService>();
            services.AddHostedService<ReportBuilderBackgroundRunHostedService>();
            services.AddScoped<OwnerCockpitService>();
            services.AddScoped<SmartSearchService>();
            services.AddScoped<GrowthIntelligenceService>();
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

    private static void RegisterVisualPartSearchService(IServiceCollection services)
    {
        services.AddHttpClient<VisualPartSearchService>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<OpenAiOptions>();
            client.BaseAddress = new Uri(options.BaseUrl, UriKind.Absolute);
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        });
    }

    public static void UseSparePartsApiPipeline(this WebApplication app)
    {
        var factory = app.Services.GetRequiredService<ISqlConnectionFactory>();
        RunMigrations(factory);

        app.UseMiddleware<ApiExceptionMiddleware>();
        app.UseCors();
        app.UseRateLimiter();
        app.UseAuthentication();
        app.UseMiddleware<WebAppUserRestrictionMiddleware>();
        app.UseMiddleware<TenantResolutionMiddleware>();
        app.UseAuthorization();
        app.MapHub<NotificationsHub>(NotificationsHubPath);
        app.MapControllers();
    }

    private static void RunMigrations(ISqlConnectionFactory factory)
    {
        TenantsMigration.EnsureApplied(factory);
        TenantIdMigration.EnsureApplied(factory);
        InvoiceNumberingMigration.EnsureApplied(factory);
        AccountingMigration.EnsureApplied(factory);
        WebAppUserRoleMigration.EnsureApplied(factory);
        UserRoleIdMigration.EnsureApplied(factory);
        MenuAccessMigration.EnsureApplied(factory);
        TransactionTypesMigration.EnsureApplied(factory);
        PartAveragePriceMigration.EnsureApplied(factory);
        PartUsedCarMigration.EnsureApplied(factory);
        CurrencyRatesMigration.EnsureApplied(factory);
        AppConstantsMigration.EnsureApplied(factory);
        CarModelsMigration.EnsureApplied(factory);
        LocationsMigration.EnsureApplied(factory);
        UsedCarsMigration.EnsureApplied(factory);
        UsedCarPartPricingMigration.EnsureApplied(factory);
        UsedCarPurchasesMigration.EnsureApplied(factory);
        UsedCarWholesaleSalesMigration.EnsureApplied(factory);
        TransactionsMigration.EnsureApplied(factory);
        BarcodeScanningMigration.EnsureApplied(factory);
        PartRequestsMigration.EnsureApplied(factory);
        PartUsedCarStockMigration.EnsureApplied(factory);
        UsedCarImagesMigration.EnsureApplied(factory);
        CommunicationsMigration.EnsureApplied(factory);
        WhatsAppCampaignsMigration.EnsureApplied(factory);
        ReportBuilderLinksMigration.EnsureApplied(factory);
        ReportBuilderAdvancedMigration.EnsureApplied(factory);
        ReorderRulesMigration.EnsureApplied(factory);
        PartSubstitutesMigration.EnsureApplied(factory);
        PartExpiryMigration.EnsureApplied(factory);
        CustomerLoyaltyMigration.EnsureApplied(factory);
        CustomerPriceTierMigration.EnsureApplied(factory);
        WarrantyClaimsMigration.EnsureApplied(factory);
        SupplierPriceHistoryMigration.EnsureApplied(factory);
        ShipmentsMigration.EnsureApplied(factory);
        ActivityLogMigration.EnsureApplied(factory);
        QuotesMigration.EnsureApplied(factory);
        CustomerCreditLimitMigration.EnsureApplied(factory);
        PricingPackagesMigration.EnsureApplied(factory);
        UserVehiclesMigration.EnsureApplied(factory);
        NeedBoardMigration.EnsureApplied(factory);
        WatchlistMigration.EnsureApplied(factory);
        SellerVerificationMigration.EnsureApplied(factory);
        MarketplaceFeaturesMigration.EnsureApplied(factory);
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


        if (IsPlaceholderJwtSecret(jwtSecret))
        {
            throw new InvalidOperationException(
                "Jwt:Secret is still set to the placeholder value. " +
                "Set a strong secret via dotnet user-secrets (development) or an environment variable (production).");
        }

        if (jwtSecret.Length < 32)
        {
            throw new InvalidOperationException(
                "Jwt:Secret must be at least 32 characters to provide sufficient signing key entropy.");
        }

        return new JwtSettings
        {
            Secret = jwtSecret,
            Issuer = jwtSection["Issuer"] ?? "SpareParts.Api",
            Audience = jwtSection["Audience"] ?? "SpareParts.Desktop",
            ExpiryHours = int.TryParse(jwtSection["ExpiryHours"], out var hours) ? hours : 12
        };
    }

    private static bool IsPlaceholderJwtSecret(string jwtSecret)
    {
        var upper = jwtSecret.ToUpperInvariant();
        return upper.StartsWith("CHANGE_ME", StringComparison.Ordinal)
            || upper.StartsWith("6533545BTWRTRWRT4H563", StringComparison.Ordinal)
            || upper.Contains("USE_ENV_OR_USER_SECRETS", StringComparison.Ordinal);
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
            Model = string.IsNullOrWhiteSpace(section["Model"]) ? "gpt-4o-mini" : section["Model"]!.Trim(),
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
