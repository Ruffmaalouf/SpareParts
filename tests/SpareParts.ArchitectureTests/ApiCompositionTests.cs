using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.DependencyInjection;
using SpareParts.Api.Controllers;
using SpareParts.Api.Hosting;
using SpareParts.Domain.Purchases;
using SpareParts.Domain.Sales;
using SpareParts.Infrastructure.Services;

namespace SpareParts.ArchitectureTests;

public class ApiCompositionTests
{
    public static TheoryData<ServiceCapability[], string[]> CapabilityControllerCases =>
        new()
        {
            { [ServiceCapability.Sales, ServiceCapability.Health], [nameof(SalesController), nameof(CustomersController), nameof(HealthController)] },
            { [ServiceCapability.Purchases, ServiceCapability.Health], [nameof(PurchasesController), nameof(SuppliersController), nameof(HealthController)] },
            { [ServiceCapability.Inventory, ServiceCapability.Health], [nameof(PartsController), nameof(WarehousesController), nameof(TransactionTypesController), nameof(HealthController)] },
            { [ServiceCapability.Identity, ServiceCapability.Health], [nameof(AuthController), nameof(UsersController), nameof(RolesController), nameof(HealthController)] },
            { [ServiceCapability.Catalog, ServiceCapability.Health], [nameof(BrandsController), nameof(CategoriesController), nameof(CarBrandsController), nameof(CarModelsController), nameof(LocationsController), nameof(UsedCarsController), nameof(CurrenciesController), nameof(AppConstantsController), nameof(HealthController)] }
        };

    [Theory]
    [MemberData(nameof(CapabilityControllerCases))]
    public void AddCapabilityControllers_ShouldExposeOnlyExpectedControllers(ServiceCapability[] capabilities, string[] expectedControllers)
    {
        var actualControllers = GetControllerNames(capabilities);

        Assert.Equal(
            expectedControllers.OrderBy(name => name, StringComparer.Ordinal).ToArray(),
            actualControllers);
    }

    [Fact]
    public void AddCapabilityControllers_WithAllCapabilities_ShouldCoverEveryConcreteApiController()
    {
        var actualControllers = GetControllerNames(Enum.GetValues<ServiceCapability>());
        var expectedControllers = typeof(SalesController).Assembly
            .GetExportedTypes()
            .Where(type =>
                type is { IsClass: true, IsAbstract: false } &&
                type.Name.EndsWith("Controller", StringComparison.Ordinal) &&
                typeof(ControllerBase).IsAssignableFrom(type))
            .Select(type => type.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(expectedControllers, actualControllers);
    }

    [Fact]
    public void AddCapabilities_ShouldRegisterSharedInvoiceServicesOnceWhenSalesAndPurchasesAreEnabled()
    {
        var services = new ServiceCollection();
        services.AddOptions();

        services.AddCapabilities(
            "SpareParts.Api",
            ServiceCapability.Sales,
            ServiceCapability.Purchases,
            ServiceCapability.Sales);

        AssertRegistrationCount<IInvoiceNumberGenerator>(services, 1);
        AssertRegistrationCount<IPaymentStatusPolicy>(services, 1);
        AssertRegistrationCount<IInvoiceTotalsCalculator>(services, 1);
        AssertRegistrationCount<IInventoryService>(services, 1);
        AssertRegistrationCount<IAccountingStrategy<SalesInvoice>>(services, 1);
        AssertRegistrationCount<IAccountingStrategy<PurchaseInvoice>>(services, 1);

        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(CustomersService));
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(SuppliersService));

        using var provider = services.BuildServiceProvider();
        var profile = provider.GetRequiredService<ServiceProfile>();

        Assert.Equal("SpareParts.Api", profile.ServiceName);
        Assert.Equal(
            [ServiceCapability.Sales, ServiceCapability.Purchases],
            profile.Capabilities);
    }

    private static string[] GetControllerNames(params ServiceCapability[] capabilities)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddCapabilityControllers(capabilities);

        using var provider = services.BuildServiceProvider();
        var manager = provider.GetRequiredService<ApplicationPartManager>();

        AddControllerAssemblyParts(manager);

        var feature = new ControllerFeature();
        manager.PopulateFeature(feature);

        return feature.Controllers
            .Select(controller => controller.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
    }

    private static void AddControllerAssemblyParts(ApplicationPartManager manager)
    {
        var assembly = typeof(SalesController).Assembly;
        var parts = ApplicationPartFactory
            .GetApplicationPartFactory(assembly)
            .GetApplicationParts(assembly);

        foreach (var part in parts.Where(part => manager.ApplicationParts.All(existing => existing.Name != part.Name)))
        {
            manager.ApplicationParts.Add(part);
        }
    }

    private static void AssertRegistrationCount<TService>(IServiceCollection services, int expectedCount)
    {
        Assert.Equal(expectedCount, services.Count(descriptor => descriptor.ServiceType == typeof(TService)));
    }
}
