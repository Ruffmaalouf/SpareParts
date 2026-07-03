using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
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
    private const string LoginUsernameItemsKey = "SpareParts.Auth.RateLimitUsername";

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
        nameof(TenantIdIndexMigration),
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
        nameof(UsedCarStateEventsMigration),
        nameof(UsedCarPartPricingMigration),
        nameof(UsedCarTeardownMigration),
        nameof(PartMarketingNotificationMigration),
        nameof(PartMarkdownMigration),
        nameof(TransactionsPaymentReminderMigration),
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
        nameof(MarketplaceFeaturesMigration),
        nameof(RepairOrdersMigration),
        nameof(GarageStockMigration),
        nameof(PartReservationsMigration),
        nameof(PartReelsMigration),
        nameof(HalfCutsMigration),
        nameof(EscrowTransactionsMigration),
        nameof(ListingBoostsMigration),
        nameof(ReferralsMigration),
        nameof(PartCompatibilityMigration),
        nameof(Phase2FeatureCodesMigration),
        nameof(ConditionCertificatesMigration),
        nameof(PartPassportPhotosMigration),
        nameof(ContentReportsMigration),
        nameof(InspectionRequestsMigration),
        nameof(PartGenealogyMigration),
        nameof(MechanicProfilesMigration),
        nameof(NewVsUsedPricingMigration),
        nameof(YardToursMigration),
        nameof(NegotiationsMigration),
        nameof(InstantOffersMigration),
        nameof(InsuranceAddonsMigration),
        nameof(ApiKeysMigration),
        nameof(Phase3FeatureCodesMigration),
        nameof(SalesReturnTypeMigration),
        nameof(PartImageEnrichmentMigration)
    ];

    private static readonly Dictionary<ServiceCapability, string[]> ControllerMap = new()
    {
        [ServiceCapability.Sales] = [nameof(SalesController), nameof(SalesReturnsController), nameof(CustomersController), nameof(WebCatalogController), nameof(LoyaltyController), nameof(CustomerPricingController), nameof(WarrantyController), nameof(ShipmentsController), nameof(QuotesController), nameof(NeedBoardController), nameof(SymptomSearchController), nameof(RepairOrdersController), nameof(HalfCutController), nameof(EscrowController), nameof(ListingBoostController), nameof(WhatsAppLinkController), nameof(PartReservationsController), nameof(PriceGeniusController), nameof(CommunityGuardController), nameof(LiveInspectionController), nameof(YardTourController), nameof(NegotiationController), nameof(InstantOfferController), nameof(PartInsuranceController)],
        [ServiceCapability.Purchases] = [nameof(PurchasesController), nameof(SuppliersController), nameof(SupplierPriceHistoryController)],
        [ServiceCapability.Inventory] = [nameof(PartsController), nameof(PartRequestsController), nameof(WarehousesController), nameof(TransactionTypesController), nameof(ScansController), nameof(ReorderController), nameof(PartSubstitutesController), nameof(PartExpiryController), nameof(WatchlistController), nameof(GarageStockController), nameof(ConditionScannerController), nameof(PartPassportController), nameof(QrTagController), nameof(PartGenealogyController), nameof(NewVsUsedController)],
        [ServiceCapability.Accounting] = [nameof(AccountsController), nameof(AccountingController)],
        [ServiceCapability.Identity] = [nameof(AuthController), nameof(UsersController), nameof(RolesController), nameof(TenantsController), nameof(SellerReputationController), nameof(SellerVerificationController), nameof(ReferralController), nameof(MechanicTrustController), nameof(ApiPlatformController)],
        [ServiceCapability.Catalog] = [nameof(BrandsController), nameof(CategoriesController), nameof(CarBrandsController), nameof(CarModelsController), nameof(LocationsController), nameof(UsedCarsController), nameof(CurrenciesController), nameof(AppConstantsController), nameof(ExcelImportController), nameof(VehicleProfileController), nameof(PartReelsController), nameof(CarCrushController), nameof(PartCompatibilityController), nameof(ArPartsFinderController)],
        [ServiceCapability.Reporting] = [nameof(ReportBuilderController), nameof(OwnerCockpitController), nameof(BusinessAssistantController), nameof(CommunicationsController), nameof(SearchController), nameof(GrowthController), nameof(ActivityLogController), nameof(MarketPriceController), nameof(VoiceSearchController), nameof(DismantlerForecastController), nameof(RegionalDemandController), nameof(PriceReportController), nameof(KareemController)],
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

        builder.Services.AddAuthorization(options =>
        {
            AuthorizationPolicies.AddRoleIdPolicies(options);

            // Framework-level fallback: every endpoint requires an authenticated user unless it explicitly
            // carries [AllowAnonymous]. This closes off any controller/action that forgets an [Authorize]
            // attribute. Confirmed safe: every action that is meant to be public (AuthController login/
            // external-login, HealthController, PricingController, WebCatalogController public actions,
            // PaymentsController.Webhook, CarBrandsController/CarModelsController image endpoints,
            // CommunicationsController inbound webhook) already carries [AllowAnonymous].
            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
        });
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
                    // Partition by IP *and* submitted username so a distributed credential-stuffing attack
                    // (many IPs hammering one account) is throttled per-account, not just per-source-IP.
                    // The username is populated into HttpContext.Items by BufferLoginUsernameMiddleware,
                    // which runs earlier in the pipeline and can safely read the body asynchronously
                    // (this partition-key factory itself is synchronous and must not touch the request body).
                    partitionKey: $"{httpCtx.Connection.RemoteIpAddress}|{GetBufferedLoginUsername(httpCtx)}",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 10,
                        Window = TimeSpan.FromMinutes(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    }));
        });
    }

    private static string GetBufferedLoginUsername(HttpContext httpContext)
        => httpContext.Items.TryGetValue(LoginUsernameItemsKey, out var value) && value is string username
            ? username
            : string.Empty;

    /// <summary>
    /// Runs before rate limiting so the auth-login partition key can include the submitted username
    /// (blunting distributed credential-stuffing across many IPs), without the rate limiter's synchronous
    /// partition-key factory touching the request body directly (Kestrel disallows synchronous body reads).
    /// Buffers and rewinds the body so downstream MVC model binding still sees the full request.
    /// </summary>
    private static async Task BufferLoginUsernameMiddleware(HttpContext context, RequestDelegate next)
    {
        if (!HttpMethods.IsPost(context.Request.Method)
            || !context.Request.Path.StartsWithSegments("/api/auth/login", StringComparison.OrdinalIgnoreCase)
            || !context.Request.HasJsonContentType())
        {
            await next(context);
            return;
        }

        try
        {
            context.Request.EnableBuffering();
            using var document = await System.Text.Json.JsonDocument.ParseAsync(
                context.Request.Body,
                new System.Text.Json.JsonDocumentOptions { AllowTrailingCommas = true },
                context.RequestAborted);
            context.Request.Body.Position = 0;

            if (document.RootElement.ValueKind == System.Text.Json.JsonValueKind.Object)
            {
                foreach (var property in document.RootElement.EnumerateObject())
                {
                    if (string.Equals(property.Name, "username", StringComparison.OrdinalIgnoreCase)
                        && property.Value.ValueKind == System.Text.Json.JsonValueKind.String)
                    {
                        context.Items[LoginUsernameItemsKey] = property.Value.GetString()?.Trim().ToLowerInvariant() ?? string.Empty;
                        break;
                    }
                }
            }
        }
        catch
        {
            // Malformed/empty body — model binding/validation downstream will reject it anyway.
            // Fall back to IP-only partitioning for this request.
        }
        finally
        {
            if (context.Request.Body.CanSeek)
            {
                context.Request.Body.Position = 0;
            }
        }

        await next(context);
    }

    public static void AddCapabilities(this IServiceCollection services, string serviceName, params ServiceCapability[] capabilities)
    {
        var distinctCapabilities = capabilities.Distinct().ToArray();

        services.AddSingleton(new ServiceProfile(serviceName, distinctCapabilities));

        // Tenant context is always registered — scoped per HTTP request, populated by TenantResolutionMiddleware.
        services.AddScoped<TenantContext>();
        services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<TenantContext>());
        services.AddScoped<TenantsService>();

        // Lets Infrastructure-layer services (e.g. PaymentProviderFactory) check IsDevelopment without a
        // direct dependency on Microsoft.Extensions.Hosting.Abstractions.
        services.AddSingleton<IRuntimeEnvironment, HostRuntimeEnvironment>();

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
            services.AddScoped<NeedBoardService>();
            services.AddScoped<SymptomSearchService>();
            services.AddScoped<RepairOrdersService>();
            services.AddScoped<HalfCutService>();
            services.AddScoped<EscrowService>();
            services.AddScoped<ListingBoostService>();
            services.AddScoped<WhatsAppLinkService>();
            services.AddScoped<PartReservationService>();
            services.TryAddScoped<MarketPriceIndexService>();
            services.AddScoped<PriceGeniusService>();
            services.AddScoped<CommunityGuardService>();
            services.AddScoped<LiveInspectionService>();
            services.AddScoped<YardTourService>();
            services.AddScoped<NegotiationBotService>();
            services.AddScoped<InstantOfferService>();
            services.AddScoped<PartInsuranceService>();
            services.AddScoped<IAccountingStrategy<SalesInvoice>>(sp =>
            {
                return new SaleAccountingStrategy(
                    sp.GetRequiredService<ISqlConnectionFactory>(),
                    sp.GetRequiredService<AccountingSettingsProvider>(),
                    sp.GetRequiredService<CustomerAccountResolver>(),
                    sp.GetRequiredService<ITenantContext>(),
                    sp.GetService<ILogger<SaleAccountingStrategy>>());
            });

            services.AddScoped<IAccountingStrategy<SalesReturn>>(sp =>
            {
                return new ReturnAccountingStrategy(
                    sp.GetRequiredService<ISqlConnectionFactory>(),
                    sp.GetRequiredService<AccountingSettingsProvider>(),
                    sp.GetRequiredService<CustomerAccountResolver>(),
                    sp.GetRequiredService<ITenantContext>(),
                    sp.GetService<ILogger<ReturnAccountingStrategy>>());
            });

            services.AddScoped<ICreateSaleHandler, CreateSaleHandler>();
            services.AddScoped<CreateSalesReturnHandler>();
            services.AddScoped<SalesService>();
            services.AddScoped<SalesReturnsService>();
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
            services.AddScoped<WatchlistService>();
            services.AddScoped<GarageStockService>();
            services.AddScoped<ConditionScannerService>();
            services.AddScoped<PartPassportService>();
            services.AddScoped<QrTagService>();
            services.AddScoped<PartGenealogyService>();
            services.AddScoped<NewVsUsedService>();
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
            services.AddScoped<SellerReputationService>();
            services.AddScoped<SellerVerificationService>();
            services.AddScoped<ReferralService>();
            services.AddScoped<MechanicTrustService>();
            services.AddScoped<ApiPlatformService>();
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
            services.AddScoped<UsedCarTwinService>();
            services.AddScoped<CurrenciesService>();
            services.AddScoped<AppConstantsService>();
            services.AddScoped<ExcelImportService>();
            services.AddScoped<UserVehicleService>();
            services.AddSingleton(new VinDecodeService());
            services.AddScoped<PartReelsService>();
            services.AddScoped<CarCrushService>();
            services.AddScoped<PartCompatibilityService>();
            services.TryAddScoped<SmartSearchService>();
            services.TryAddScoped<ScanLookupService>();
            services.AddScoped<ArPartsFinderService>();
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
            services.AddScoped<MarketPriceIndexService>();
            services.AddScoped<DismantlerForecastService>();
            services.AddScoped<RegionalDemandService>();
            services.AddScoped<PriceReportService>();
            services.AddScoped<KareemConciergeService>();
        }

        // The Intake & Teardown Agent needs UsedCarsService/CarCrushService (Catalog) together
        // with GarageStockService/PartGenealogyService (Inventory), so it can only run where both
        // capabilities are present together — i.e. the monolithic SpareParts.Api host.
        if (distinctCapabilities.Contains(ServiceCapability.Catalog)
            && distinctCapabilities.Contains(ServiceCapability.Inventory))
        {
            services.AddHostedService<IntakeTeardownAgentHostedService>();
        }

        // The Pricing Agent additionally needs MarketPriceIndexService, and the Marketing Agent
        // needs CommunicationsService — both live under the Reporting capability, so both agents
        // require Catalog + Inventory + Reporting together.
        if (distinctCapabilities.Contains(ServiceCapability.Catalog)
            && distinctCapabilities.Contains(ServiceCapability.Inventory)
            && distinctCapabilities.Contains(ServiceCapability.Reporting))
        {
            services.AddScoped<PartAutoPricingService>();
            services.AddHostedService<PricingAgentHostedService>();

            services.AddScoped<DemandMatchingService>();
            services.AddHostedService<MarketingAgentHostedService>();

            services.AddScoped<DeadStockMarkdownService>();
            services.AddHostedService<DeadStockMarkdownAgentHostedService>();

            services.AddScoped<BuyingAdvisorService>();
            services.AddHostedService<BuyingAdvisorAgentHostedService>();

            services.AddScoped<ArCollectionsService>();
            services.AddHostedService<ArCollectionsAgentHostedService>();

            services.AddScoped<OwnerCockpitDigestService>();
            services.AddHostedService<OwnerCockpitDigestAgentHostedService>();
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

        app.UseMiddleware<SecurityHeadersMiddleware>();
        app.UseMiddleware<ApiExceptionMiddleware>();
        app.UseCors();
        app.Use(BufferLoginUsernameMiddleware);
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
        // Ledger table + short-circuit: each migration below only re-executes its full body the
        // first time it succeeds; afterwards RunOnce sees it recorded in dbo.SchemaMigrations and
        // skips straight past it. Every migration's own internal idempotency guards (IF OBJECT_ID
        // IS NULL, IF NOT EXISTS, etc.) are left untouched as a safety net — they still make each
        // migration safe to re-run if its ledger row is ever missing.
        //
        // TenantsMigration and TenantIdMigration intentionally run every startup, unguarded by the
        // ledger: TenantIdMigration's backfill (`UPDATE ... SET TenantId = @TenantId WHERE TenantId
        // IS NULL`) must keep running for any new tenant-owned tables/rows created after the first
        // deploy, not just once ever, and TenantsMigration is cheap/idempotent and precedes it.
        TenantsMigration.EnsureApplied(factory);
        TenantIdMigration.EnsureApplied(factory);

        SchemaMigrationLedger.EnsureTableExists(factory);

        SchemaMigrationLedger.RunOnce(factory, nameof(InvoiceNumberingMigration), InvoiceNumberingMigration.EnsureApplied);
        SchemaMigrationLedger.RunOnce(factory, nameof(AccountingMigration), AccountingMigration.EnsureApplied);
        SchemaMigrationLedger.RunOnce(factory, nameof(WebAppUserRoleMigration), WebAppUserRoleMigration.EnsureApplied);
        SchemaMigrationLedger.RunOnce(factory, nameof(UserRoleIdMigration), UserRoleIdMigration.EnsureApplied);
        SchemaMigrationLedger.RunOnce(factory, nameof(MenuAccessMigration), MenuAccessMigration.EnsureApplied);
        SchemaMigrationLedger.RunOnce(factory, nameof(TransactionTypesMigration), TransactionTypesMigration.EnsureApplied);
        SchemaMigrationLedger.RunOnce(factory, nameof(PartAveragePriceMigration), PartAveragePriceMigration.EnsureApplied);
        SchemaMigrationLedger.RunOnce(factory, nameof(PartUsedCarMigration), PartUsedCarMigration.EnsureApplied);
        SchemaMigrationLedger.RunOnce(factory, nameof(CurrencyRatesMigration), CurrencyRatesMigration.EnsureApplied);
        SchemaMigrationLedger.RunOnce(factory, nameof(AppConstantsMigration), AppConstantsMigration.EnsureApplied);
        SchemaMigrationLedger.RunOnce(factory, nameof(CarModelsMigration), CarModelsMigration.EnsureApplied);
        SchemaMigrationLedger.RunOnce(factory, nameof(LocationsMigration), LocationsMigration.EnsureApplied);
        SchemaMigrationLedger.RunOnce(factory, nameof(UsedCarsMigration), UsedCarsMigration.EnsureApplied);
        SchemaMigrationLedger.RunOnce(factory, nameof(UsedCarStateEventsMigration), UsedCarStateEventsMigration.EnsureApplied);
        SchemaMigrationLedger.RunOnce(factory, nameof(UsedCarPartPricingMigration), UsedCarPartPricingMigration.EnsureApplied);
        SchemaMigrationLedger.RunOnce(factory, nameof(UsedCarTeardownMigration), UsedCarTeardownMigration.EnsureApplied);
        SchemaMigrationLedger.RunOnce(factory, nameof(PartMarketingNotificationMigration), PartMarketingNotificationMigration.EnsureApplied);
        SchemaMigrationLedger.RunOnce(factory, nameof(PartMarkdownMigration), PartMarkdownMigration.EnsureApplied);
        SchemaMigrationLedger.RunOnce(factory, nameof(TransactionsPaymentReminderMigration), TransactionsPaymentReminderMigration.EnsureApplied);
        SchemaMigrationLedger.RunOnce(factory, nameof(UsedCarPurchasesMigration), UsedCarPurchasesMigration.EnsureApplied);
        SchemaMigrationLedger.RunOnce(factory, nameof(UsedCarWholesaleSalesMigration), UsedCarWholesaleSalesMigration.EnsureApplied);
        SchemaMigrationLedger.RunOnce(factory, nameof(TransactionsMigration), TransactionsMigration.EnsureApplied);
        SchemaMigrationLedger.RunOnce(factory, nameof(BarcodeScanningMigration), BarcodeScanningMigration.EnsureApplied);
        SchemaMigrationLedger.RunOnce(factory, nameof(PartRequestsMigration), PartRequestsMigration.EnsureApplied);
        SchemaMigrationLedger.RunOnce(factory, nameof(PartUsedCarStockMigration), PartUsedCarStockMigration.EnsureApplied);
        SchemaMigrationLedger.RunOnce(factory, nameof(UsedCarImagesMigration), UsedCarImagesMigration.EnsureApplied);
        SchemaMigrationLedger.RunOnce(factory, nameof(CommunicationsMigration), CommunicationsMigration.EnsureApplied);
        SchemaMigrationLedger.RunOnce(factory, nameof(WhatsAppCampaignsMigration), WhatsAppCampaignsMigration.EnsureApplied);
        SchemaMigrationLedger.RunOnce(factory, nameof(ReportBuilderLinksMigration), ReportBuilderLinksMigration.EnsureApplied);
        SchemaMigrationLedger.RunOnce(factory, nameof(ReportBuilderAdvancedMigration), ReportBuilderAdvancedMigration.EnsureApplied);
        SchemaMigrationLedger.RunOnce(factory, nameof(ReorderRulesMigration), ReorderRulesMigration.EnsureApplied);
        SchemaMigrationLedger.RunOnce(factory, nameof(PartSubstitutesMigration), PartSubstitutesMigration.EnsureApplied);
        SchemaMigrationLedger.RunOnce(factory, nameof(PartExpiryMigration), PartExpiryMigration.EnsureApplied);
        SchemaMigrationLedger.RunOnce(factory, nameof(CustomerLoyaltyMigration), CustomerLoyaltyMigration.EnsureApplied);
        SchemaMigrationLedger.RunOnce(factory, nameof(CustomerPriceTierMigration), CustomerPriceTierMigration.EnsureApplied);
        SchemaMigrationLedger.RunOnce(factory, nameof(WarrantyClaimsMigration), WarrantyClaimsMigration.EnsureApplied);
        SchemaMigrationLedger.RunOnce(factory, nameof(SupplierPriceHistoryMigration), SupplierPriceHistoryMigration.EnsureApplied);
        SchemaMigrationLedger.RunOnce(factory, nameof(ShipmentsMigration), ShipmentsMigration.EnsureApplied);
        SchemaMigrationLedger.RunOnce(factory, nameof(ActivityLogMigration), ActivityLogMigration.EnsureApplied);
        SchemaMigrationLedger.RunOnce(factory, nameof(QuotesMigration), QuotesMigration.EnsureApplied);
        SchemaMigrationLedger.RunOnce(factory, nameof(CustomerCreditLimitMigration), CustomerCreditLimitMigration.EnsureApplied);
        SchemaMigrationLedger.RunOnce(factory, nameof(PricingPackagesMigration), PricingPackagesMigration.EnsureApplied);
        SchemaMigrationLedger.RunOnce(factory, nameof(UserVehiclesMigration), UserVehiclesMigration.EnsureApplied);
        SchemaMigrationLedger.RunOnce(factory, nameof(NeedBoardMigration), NeedBoardMigration.EnsureApplied);
        SchemaMigrationLedger.RunOnce(factory, nameof(WatchlistMigration), WatchlistMigration.EnsureApplied);
        SchemaMigrationLedger.RunOnce(factory, nameof(SellerVerificationMigration), SellerVerificationMigration.EnsureApplied);
        SchemaMigrationLedger.RunOnce(factory, nameof(MarketplaceFeaturesMigration), MarketplaceFeaturesMigration.EnsureApplied);
        SchemaMigrationLedger.RunOnce(factory, nameof(RepairOrdersMigration), RepairOrdersMigration.EnsureApplied);
        SchemaMigrationLedger.RunOnce(factory, nameof(GarageStockMigration), GarageStockMigration.EnsureApplied);
        SchemaMigrationLedger.RunOnce(factory, nameof(PartReservationsMigration), PartReservationsMigration.EnsureApplied);
        SchemaMigrationLedger.RunOnce(factory, nameof(PartReelsMigration), PartReelsMigration.EnsureApplied);
        SchemaMigrationLedger.RunOnce(factory, nameof(HalfCutsMigration), HalfCutsMigration.EnsureApplied);
        SchemaMigrationLedger.RunOnce(factory, nameof(EscrowTransactionsMigration), EscrowTransactionsMigration.EnsureApplied);
        SchemaMigrationLedger.RunOnce(factory, nameof(ListingBoostsMigration), ListingBoostsMigration.EnsureApplied);
        SchemaMigrationLedger.RunOnce(factory, nameof(ReferralsMigration), ReferralsMigration.EnsureApplied);
        SchemaMigrationLedger.RunOnce(factory, nameof(PartCompatibilityMigration), PartCompatibilityMigration.EnsureApplied);
        SchemaMigrationLedger.RunOnce(factory, nameof(Phase2FeatureCodesMigration), Phase2FeatureCodesMigration.EnsureApplied);
        SchemaMigrationLedger.RunOnce(factory, nameof(ConditionCertificatesMigration), ConditionCertificatesMigration.EnsureApplied);
        SchemaMigrationLedger.RunOnce(factory, nameof(PartPassportPhotosMigration), PartPassportPhotosMigration.EnsureApplied);
        SchemaMigrationLedger.RunOnce(factory, nameof(ContentReportsMigration), ContentReportsMigration.EnsureApplied);
        SchemaMigrationLedger.RunOnce(factory, nameof(InspectionRequestsMigration), InspectionRequestsMigration.EnsureApplied);
        SchemaMigrationLedger.RunOnce(factory, nameof(PartGenealogyMigration), PartGenealogyMigration.EnsureApplied);
        SchemaMigrationLedger.RunOnce(factory, nameof(MechanicProfilesMigration), MechanicProfilesMigration.EnsureApplied);
        SchemaMigrationLedger.RunOnce(factory, nameof(NewVsUsedPricingMigration), NewVsUsedPricingMigration.EnsureApplied);
        SchemaMigrationLedger.RunOnce(factory, nameof(YardToursMigration), YardToursMigration.EnsureApplied);
        SchemaMigrationLedger.RunOnce(factory, nameof(NegotiationsMigration), NegotiationsMigration.EnsureApplied);
        SchemaMigrationLedger.RunOnce(factory, nameof(InstantOffersMigration), InstantOffersMigration.EnsureApplied);
        SchemaMigrationLedger.RunOnce(factory, nameof(InsuranceAddonsMigration), InsuranceAddonsMigration.EnsureApplied);
        SchemaMigrationLedger.RunOnce(factory, nameof(ApiKeysMigration), ApiKeysMigration.EnsureApplied);
        SchemaMigrationLedger.RunOnce(factory, nameof(Phase3FeatureCodesMigration), Phase3FeatureCodesMigration.EnsureApplied);
        SchemaMigrationLedger.RunOnce(factory, nameof(SalesReturnTypeMigration), SalesReturnTypeMigration.EnsureApplied);
        SchemaMigrationLedger.RunOnce(factory, nameof(PartImageEnrichmentMigration), PartImageEnrichmentMigration.EnsureApplied);

        // TenantIdIndexMigration (fix #3) intentionally runs unguarded by the ledger too: it is
        // itself fully idempotent (IF NOT EXISTS on sys.indexes) and cheap once indexes exist, and
        // must remain able to add an index for a table that becomes high-volume later without
        // requiring a ledger-row reset.
        TenantIdIndexMigration.EnsureApplied(factory);
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
