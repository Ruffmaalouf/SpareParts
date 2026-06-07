using SpareParts.Desktop.Wpf;
using SpareParts.Desktop.Wpf.Interfaces;
using SpareParts.Desktop.Wpf.Management;
using SpareParts.Domain.BusinessPartners;
using SpareParts.Domain.Cars;
using SpareParts.Domain.Inventory;
using SpareParts.Domain.MasterData;
using SpareParts.Domain.Sales;
using Xunit;

namespace SpareParts.ManagementTests;

public sealed class ManagementCoordinatorCrudTests
{
    [Theory]
    [MemberData(nameof(CreateSaveCases))]
    public async Task Save_create_cases_hit_expected_post_endpoint(
        string caseName,
        Func<ManagementCoordinator, Task<ManagementOperationResult>> action,
        string expectedUrl,
        Type expectedPayloadType)
    {
        var crud = new RecordingCrudApiClient();
        var sut = CreateSut(crud);

        var result = await action(sut);

        Assert.True(result.Success, $"{caseName}: {result.Message}");
        Assert.Equal("POST", crud.LastMethod);
        Assert.Equal(expectedUrl, crud.LastUrl);
        Assert.NotNull(crud.LastPayload);
        Assert.True(expectedPayloadType.IsInstanceOfType(crud.LastPayload), $"{caseName}: unexpected payload type {crud.LastPayload?.GetType().FullName}");
    }

    [Theory]
    [MemberData(nameof(UpdateSaveCases))]
    public async Task Save_update_cases_hit_expected_put_endpoint(
        string caseName,
        Func<ManagementCoordinator, Task<ManagementOperationResult>> action,
        string expectedUrl,
        Type expectedPayloadType)
    {
        var crud = new RecordingCrudApiClient();
        var sut = CreateSut(crud);

        var result = await action(sut);

        Assert.True(result.Success, $"{caseName}: {result.Message}");
        Assert.Equal("PUT", crud.LastMethod);
        Assert.Equal(expectedUrl, crud.LastUrl);
        Assert.NotNull(crud.LastPayload);
        Assert.True(expectedPayloadType.IsInstanceOfType(crud.LastPayload), $"{caseName}: unexpected payload type {crud.LastPayload?.GetType().FullName}");
    }

    [Theory]
    [MemberData(nameof(DeleteCases))]
    public async Task Delete_cases_hit_expected_delete_endpoint(
        string caseName,
        Func<ManagementCoordinator, Task<ManagementOperationResult>> action,
        string expectedUrl)
    {
        var crud = new RecordingCrudApiClient();
        var sut = CreateSut(crud);

        var result = await action(sut);

        Assert.True(result.Success, $"{caseName}: {result.Message}");
        Assert.Equal("DELETE", crud.LastMethod);
        Assert.Equal(expectedUrl, crud.LastUrl);
        Assert.Null(crud.LastPayload);
    }

    [Fact]
    public async Task SaveWarehouseAsync_requires_name()
    {
        var sut = CreateSut(new RecordingCrudApiClient());

        var result = await sut.SaveWarehouseAsync(new WarehouseManagementViewModel(NullManagementFeatureContext.Instance));

        Assert.False(result.Success);
        Assert.Contains("Warehouse name is required", result.Message);
    }

    [Fact]
    public async Task DeleteWarehouseAsync_requires_selected_row()
    {
        var sut = CreateSut(new RecordingCrudApiClient());

        var result = await sut.DeleteWarehouseAsync(null);

        Assert.False(result.Success);
        Assert.Contains("Select a warehouse to delete", result.Message);
    }

    [Fact]
    public async Task GeneratePartNotesAsync_updates_feature_notes_from_ai_response()
    {
        var crud = new RecordingCrudApiClient();
        var partsApi = new StubPartsApiClient
        {
            Response = new GeneratePartNotesResponse
            {
                SuggestedNotes = "OEM-aligned replacement part suitable for general brake service work.",
                SuggestedAveragePrice = 37.5m
            }
        };

        var sut = CreateSut(crud, partsApi);
        var feature = new PartManagementViewModel(NullManagementFeatureContext.Instance, new NullFilePickerService(), new NullUserNotificationService(), new NullPartWorkspaceService())
        {
            NewPartCode = "BP-100",
            NewPartName = "Brake Pad",
            NewPartOEM = "OEM-7781",
            NewPartCategoryId = 3,
            NewPartBrandId = 8,
            NewPartCurrency = "usd"
        };

        var result = await sut.GeneratePartNotesAsync(
            feature,
            new Dictionary<int, string> { [3] = "Braking" },
            new Dictionary<int, string> { [8] = "Bosch" });

        Assert.True(result.Success, result.Message);
        Assert.Equal(partsApi.Response.SuggestedNotes, feature.NewPartNotes);
        Assert.Equal("37.5", feature.NewPartAveragePrice);
        Assert.NotNull(partsApi.LastRequest);
        Assert.Equal("Braking", partsApi.LastRequest!.CategoryName);
        Assert.Equal("Bosch", partsApi.LastRequest.BrandName);
        Assert.Equal("USD", partsApi.LastRequest.Currency);
    }

    public static IEnumerable<object[]> CreateSaveCases()
    {
        yield return SaveCase(
            "customer-create",
            sut => sut.SaveCustomerAsync(new CustomerManagementViewModel(NullManagementFeatureContext.Instance)
            {
                NewCustomerName = "Acme Customer",
                NewCustomerPhone = "123456"
            }),
            "api/customers",
            typeof(CreateCustomerRequest));

        yield return SaveCase(
            "supplier-create",
            sut => sut.SaveSupplierAsync(new SupplierManagementViewModel(NullManagementFeatureContext.Instance)
            {
                NewSupplierName = "Acme Supplier",
                NewSupplierEmail = "supplier@example.com"
            }),
            "api/suppliers",
            typeof(CreateSupplierRequest));

        yield return SaveCase(
            "brand-create",
            sut => sut.SaveBrandAsync(new BrandManagementViewModel(NullManagementFeatureContext.Instance)
            {
                NewBrandName = "Bosch",
                NewBrandIsActive = true
            }),
            "api/brands",
            typeof(CreateBrandRequest));

        yield return SaveCase(
            "part-create",
            sut => sut.SavePartAsync(new PartManagementViewModel(NullManagementFeatureContext.Instance, new NullFilePickerService(), new NullUserNotificationService(), new NullPartWorkspaceService())
            {
                NewPartCode = "P-001",
                NewPartName = "Brake Pad",
                NewPartCategoryId = 2,
                NewPartCurrency = "USD"
            }),
            "api/parts",
            typeof(CreatePartRequest));

        yield return SaveCase(
            "car-brand-create",
            sut => sut.SaveCarBrandAsync(new CarModelManagementViewModel(NullManagementFeatureContext.Instance)
            {
                NewCarBrandName = "Toyota",
                NewCarBrandCountry = "Japan",
                NewCarBrandRegionGroup = "Asia"
            }),
            "api/carbrands",
            typeof(CreateCarBrandRequest));

        yield return SaveCase(
            "car-model-create",
            sut => sut.SaveCarModelAsync(new CarModelManagementViewModel(NullManagementFeatureContext.Instance)
            {
                NewCarModelBrandId = 9,
                NewCarModelName = "Corolla",
                NewCarModelBodyType = "Sedan"
            }),
            "api/carmodels",
            typeof(CreateCarModelRequest));

        yield return SaveCase(
            "location-create",
            sut => sut.SaveLocationAsync(new LocationManagementViewModel(NullManagementFeatureContext.Instance)
            {
                NewLocationName = "Beirut Port",
                NewLocationShippingFees = 125m,
                NewLocationShippingFeesCurrencyCode = "USD"
            }),
            "api/locations",
            typeof(CreateLocationRequest));

        yield return SaveCase(
            "warehouse-create",
            sut => sut.SaveWarehouseAsync(new WarehouseManagementViewModel(NullManagementFeatureContext.Instance)
            {
                NewWarehouseName = "Main Warehouse",
                NewWarehouseAddress = "Industrial Road",
                NewWarehouseIsMain = true
            }),
            "api/warehouses",
            typeof(CreateWarehouseRequest));

        yield return SaveCase(
            "transaction-type-create",
            sut => sut.SaveTransactionTypeAsync(new TransactionTypeManagementViewModel(NullManagementFeatureContext.Instance)
            {
                NewTransactionTypeName = "Retail Sale",
                NewTransactionCurrencyCode = "USD",
                NewTransactionCounterRate = 1.5m,
                NewTransactionIsActive = true
            }),
            "api/transactiontypes",
            typeof(CreateTransactionTypeRequest));

        yield return SaveCase(
            "used-car-create",
            sut => sut.SaveUsedCarAsync(new CreateUsedCarRequest
            {
                SupplierId = 6,
                CarModelId = 3,
                ModelYear = 2022,
                PriceCurrency = "USD",
                Price = 10000m,
                LocationId = 5,
                Shipping = 0m,
                Customs = 0m
            }, null),
            "api/usedcars",
            typeof(CreateUsedCarRequest));
    }

    public static IEnumerable<object[]> UpdateSaveCases()
    {
        yield return SaveCase(
            "customer-update",
            sut => sut.SaveCustomerAsync(new CustomerManagementViewModel(NullManagementFeatureContext.Instance)
            {
                SelectedCustomer = new CustomerDto { Id = 7 },
                NewCustomerName = "Updated Customer"
            }),
            "api/customers/7",
            typeof(CreateCustomerRequest));

        yield return SaveCase(
            "supplier-update",
            sut => sut.SaveSupplierAsync(new SupplierManagementViewModel(NullManagementFeatureContext.Instance)
            {
                SelectedSupplier = new SupplierDto { Id = 9 },
                NewSupplierName = "Updated Supplier",
                NewSupplierEmail = "updated-supplier@example.com"
            }),
            "api/suppliers/9",
            typeof(CreateSupplierRequest));

        yield return SaveCase(
            "location-update",
            sut => sut.SaveLocationAsync(new LocationManagementViewModel(NullManagementFeatureContext.Instance)
            {
                SelectedLocation = new LocationDto { LocationId = 11 },
                NewLocationName = "Updated Location",
                NewLocationShippingFees = 22m,
                NewLocationShippingFeesCurrencyCode = "EUR"
            }),
            "api/locations/11",
            typeof(CreateLocationRequest));

        yield return SaveCase(
            "warehouse-update",
            sut => sut.SaveWarehouseAsync(new WarehouseManagementViewModel(NullManagementFeatureContext.Instance)
            {
                SelectedWarehouse = new WarehouseDto { Id = 15 },
                NewWarehouseName = "Updated Warehouse",
                NewWarehouseAddress = "North Yard",
                NewWarehouseIsMain = false
            }),
            "api/warehouses/15",
            typeof(CreateWarehouseRequest));

        yield return SaveCase(
            "transaction-type-update",
            sut => sut.SaveTransactionTypeAsync(new TransactionTypeManagementViewModel(NullManagementFeatureContext.Instance)
            {
                SelectedTransactionType = new TransactionTypeDto { Id = 4 },
                NewTransactionTypeName = "Wholesale",
                NewTransactionCurrencyCode = "USD",
                NewTransactionCounterRate = 2m,
                NewTransactionIsActive = false
            }),
            "api/transactiontypes/4",
            typeof(CreateTransactionTypeRequest));

        yield return SaveCase(
            "used-car-update",
            sut => sut.SaveUsedCarAsync(new CreateUsedCarRequest
            {
                SupplierId = 6,
                CarModelId = 8,
                ModelYear = 2020,
                PriceCurrency = "USD",
                Price = 8000m,
                LocationId = 2,
                Shipping = 100m,
                Customs = 50m
            }, new UsedCarEntry { Id = 13 }),
            "api/usedcars/13",
            typeof(CreateUsedCarRequest));
    }

    public static IEnumerable<object[]> DeleteCases()
    {
        yield return DeleteCase("customer-delete", sut => sut.DeleteCustomerAsync(new CustomerDto { Id = 7 }), "api/customers/7");
        yield return DeleteCase("supplier-delete", sut => sut.DeleteSupplierAsync(new SupplierDto { Id = 9 }), "api/suppliers/9");
        yield return DeleteCase("brand-delete", sut => sut.DeleteBrandAsync(new BrandDto { Id = 4 }), "api/brands/4");
        yield return DeleteCase("part-delete", sut => sut.DeletePartAsync(new PartDto { Id = 12 }), "api/parts/12");
        yield return DeleteCase("car-brand-delete", sut => sut.DeleteCarBrandAsync(new CarBrandDto { Id = 5 }), "api/carbrands/5");
        yield return DeleteCase("car-model-delete", sut => sut.DeleteCarModelAsync(new CarModelDto { Id = 6 }), "api/carmodels/6");
        yield return DeleteCase("location-delete", sut => sut.DeleteLocationAsync(new LocationDto { LocationId = 14 }), "api/locations/14");
        yield return DeleteCase("warehouse-delete", sut => sut.DeleteWarehouseAsync(new WarehouseDto { Id = 10 }), "api/warehouses/10");
        yield return DeleteCase("transaction-type-delete", sut => sut.DeleteTransactionTypeAsync(new TransactionTypeDto { Id = 3 }), "api/transactiontypes/3");
        yield return DeleteCase("used-car-delete", sut => sut.DeleteUsedCarAsync(new UsedCarEntry { Id = 2 }), "api/usedcars/2");
    }

    private static object[] SaveCase(
        string caseName,
        Func<ManagementCoordinator, Task<ManagementOperationResult>> action,
        string expectedUrl,
        Type expectedPayloadType)
        => new object[] { caseName, action, expectedUrl, expectedPayloadType };

    private static object[] DeleteCase(
        string caseName,
        Func<ManagementCoordinator, Task<ManagementOperationResult>> action,
        string expectedUrl)
        => new object[] { caseName, action, expectedUrl };

    private static ManagementCoordinator CreateSut(RecordingCrudApiClient crud, IPartsApiClient? partsApi = null)
        => new(crud, new StubCarCatalogApiClient(), partsApi ?? new StubPartsApiClient());
}
