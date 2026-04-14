using SpareParts.Domain.BusinessPartners;
using SpareParts.Domain.Cars;
using SpareParts.Domain.Inventory;
using SpareParts.Domain.MasterData;
using SpareParts.Desktop.Wpf.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SpareParts.Desktop.Wpf.Management
{
    public class ManagementCoordinator
    {
        private readonly ICrudApiClient _crudApi;
        private readonly ICarCatalogApiClient _carCatalogApi;

        public ManagementCoordinator(ICrudApiClient crudApi, ICarCatalogApiClient carCatalogApi)
        {
            _crudApi = crudApi;
            _carCatalogApi = carCatalogApi;
        }

        public async Task<ManagementLoadResult> LoadAllAsync(RolesViewModel rolesVm)
        {
            var customers = await _crudApi.GetAllAsync<CustomerDto>("api/customers");
            var suppliers = await _crudApi.GetAllAsync<SupplierDto>("api/suppliers");
            var brands = await _crudApi.GetAllAsync<BrandDto>("api/brands");
            var carBrands = await _carCatalogApi.GetCarBrandsAsync();
            var categories = await _crudApi.GetAllAsync<CategoryDto>("api/categories");
            var parts = await _crudApi.GetAllAsync<PartDto>("api/parts");
            var carModels = await _crudApi.GetAllAsync<CarModelDto>("api/carmodels");
            var locations = await _crudApi.GetAllAsync<LocationDto>("api/locations");
            var usedCars = await _crudApi.GetAllAsync<UsedCarDto>("api/usedcars");
            var warehouses = await _crudApi.GetAllAsync<WarehouseDto>("api/warehouses");
            var currencyRates = await _crudApi.GetAllAsync<CurrencyRateDto>("api/currencies");
            var transactionTypes = await _crudApi.GetAllAsync<TransactionTypeDto>("api/transactiontypes");
            var appConstants = await _crudApi.GetAllAsync<AppConstantDto>("api/appconstants");
            await rolesVm.LoadAsync();

            return new ManagementLoadResult
            {
                Customers = customers,
                Suppliers = suppliers,
                Brands = brands,
                CarBrands = carBrands,
                Categories = categories,
                Parts = parts,
                CarModels = carModels,
                Locations = locations,
                UsedCars = usedCars,
                Warehouses = warehouses,
                CurrencyRates = currencyRates,
                TransactionTypes = transactionTypes,
                AppConstants = appConstants
            };
        }

        public Task<ManagementOperationResult> SaveCustomerAsync(CustomerManagementViewModel feature)
        {
            if (string.IsNullOrWhiteSpace(feature.NewCustomerName))
            {
                return Task.FromResult(ToFailure(new DomainValidationException("Customer name is required.", "customer_name_required"), "saving Customer"));
            }

            var payload = new CreateCustomerRequest
            {
                Name = feature.NewCustomerName,
                Phone = feature.NewCustomerPhone,
                Email = feature.NewCustomerEmail,
                Address = feature.NewCustomerAddress,
                TaxNumber = feature.NewCustomerTax,
                OpeningBalance = feature.NewCustomerBalance
            };

            return SaveAsync(
                feature.SelectedCustomer is { Id: > 0 },
                feature.SelectedCustomer?.Id,
                "api/customers",
                payload,
                "Customer");
        }

        public Task<ManagementOperationResult> DeleteCustomerAsync(CustomerDto? selected)
        {
            if (selected is not { Id: > 0 })
            {
                return Task.FromResult(ToFailure(new DomainValidationException("Select a customer to delete.", "customer_selection_required"), "deleting Customer"));
            }

            return DeleteAsync($"api/customers/{selected.Id}", "Customer");
        }

        public Task<ManagementOperationResult> SaveSupplierAsync(SupplierManagementViewModel feature)
        {
            if (string.IsNullOrWhiteSpace(feature.NewSupplierName))
            {
                return Task.FromResult(ToFailure(new DomainValidationException("Supplier name is required.", "supplier_name_required"), "saving Supplier"));
            }

            var payload = new CreateSupplierRequest
            {
                Name = feature.NewSupplierName,
                Phone = feature.NewSupplierPhone,
                Email = feature.NewSupplierEmail,
                Address = feature.NewSupplierAddress,
                TaxNumber = feature.NewSupplierTax,
                OpeningBalance = feature.NewSupplierBalance
            };

            return SaveAsync(
                feature.SelectedSupplier is { Id: > 0 },
                feature.SelectedSupplier?.Id,
                "api/suppliers",
                payload,
                "Supplier");
        }

        public Task<ManagementOperationResult> DeleteSupplierAsync(SupplierDto? selected)
        {
            if (selected is not { Id: > 0 })
            {
                return Task.FromResult(ToFailure(new DomainValidationException("Select a supplier to delete.", "supplier_selection_required"), "deleting Supplier"));
            }

            return DeleteAsync($"api/suppliers/{selected.Id}", "Supplier");
        }

        public Task<ManagementOperationResult> SaveBrandAsync(BrandManagementViewModel feature)
        {
            if (string.IsNullOrWhiteSpace(feature.NewBrandName))
            {
                return Task.FromResult(ToFailure(new DomainValidationException("Brand name is required.", "brand_name_required"), "saving Brand"));
            }

            var payload = new CreateBrandRequest
            {
                Name = feature.NewBrandName,
                IsActive = feature.NewBrandIsActive
            };

            return SaveAsync(
                feature.SelectedBrand is { Id: > 0 },
                feature.SelectedBrand?.Id,
                "api/brands",
                payload,
                "Brand");
        }

        public Task<ManagementOperationResult> DeleteBrandAsync(BrandDto? selected)
        {
            if (selected is not { Id: > 0 })
            {
                return Task.FromResult(ToFailure(new DomainValidationException("Select a brand to delete.", "brand_selection_required"), "deleting Brand"));
            }

            return DeleteAsync($"api/brands/{selected.Id}", "Brand");
        }

        public Task<ManagementOperationResult> SavePartAsync(PartManagementViewModel feature)
        {
            if (string.IsNullOrWhiteSpace(feature.NewPartCode) || string.IsNullOrWhiteSpace(feature.NewPartName))
            {
                return Task.FromResult(ToFailure(new DomainValidationException("Part code and name are required.", "part_required_fields_missing"), "saving Part"));
            }

            var payload = new CreatePartRequest
            {
                InternalCode = feature.NewPartCode,
                Name = feature.NewPartName,
                OEMNumber = feature.NewPartOEM,
                Condition = PartCondition.New,
                CategoryId = feature.NewPartCategoryId,
                BrandId = feature.NewPartBrandId,
                CostPrice = feature.NewPartCostPrice,
                SalePrice = feature.NewPartSalePrice,
                Currency = feature.NewPartCurrency,
                MinStock = feature.NewPartMinStock,
                Notes = feature.NewPartNotes
            };

            return SaveAsync(
                feature.SelectedPart is { Id: > 0 },
                feature.SelectedPart?.Id,
                "api/parts",
                payload,
                "Part");
        }

        public Task<ManagementOperationResult> DeletePartAsync(PartDto? selected)
        {
            if (selected is not { Id: > 0 })
            {
                return Task.FromResult(ToFailure(new DomainValidationException("Select a part to delete.", "part_selection_required"), "deleting Part"));
            }

            return DeleteAsync($"api/parts/{selected.Id}", "Part");
        }


        public Task<ManagementOperationResult> SaveCarBrandAsync(CarModelManagementViewModel feature)
        {
            if (string.IsNullOrWhiteSpace(feature.NewCarBrandName))
            {
                return Task.FromResult(ToFailure(new DomainValidationException("Car brand name is required.", "car_brand_name_required"), "saving Car brand"));
            }

            var payload = new CreateCarBrandRequest
            {
                Name = feature.NewCarBrandName.Trim(),
                Country = feature.NewCarBrandCountry.Trim(),
                RegionGroup = feature.NewCarBrandRegionGroup.Trim(),
                SortOrder = feature.NewCarBrandSortOrder
            };

            return SaveAsync(
                feature.SelectedCarBrand is { Id: > 0 },
                feature.SelectedCarBrand?.Id,
                "api/carbrands",
                payload,
                "Car brand");
        }

        public Task<ManagementOperationResult> DeleteCarBrandAsync(CarBrandDto? selected)
        {
            if (selected is not { Id: > 0 })
            {
                return Task.FromResult(ToFailure(new DomainValidationException("Select a car brand to delete.", "car_brand_selection_required"), "deleting Car brand"));
            }

            return DeleteAsync($"api/carbrands/{selected.Id}", "Car brand");
        }

        public Task<ManagementOperationResult> SaveCarModelAsync(CarModelManagementViewModel feature)
        {
            if (feature.NewCarModelBrandId <= 0)
            {
                return Task.FromResult(ToFailure(new DomainValidationException("Car brand is required.", "car_model_brand_required"), "saving Car Model"));
            }

            if (string.IsNullOrWhiteSpace(feature.NewCarModelName))
            {
                return Task.FromResult(ToFailure(new DomainValidationException("Car model name is required.", "car_model_name_required"), "saving Car Model"));
            }

            var payload = new CreateCarModelRequest
            {
                Name = feature.NewCarModelName.Trim(),
                BodyType = feature.NewCarModelBodyType.Trim(),
                CarBrandId = feature.NewCarModelBrandId
            };

            return SaveAsync(
                feature.SelectedCarModel is { Id: > 0 },
                feature.SelectedCarModel?.Id,
                "api/carmodels",
                payload,
                "Car Model");
        }

        public Task<ManagementOperationResult> DeleteCarModelAsync(CarModelDto? selected)
        {
            if (selected is not { Id: > 0 })
            {
                return Task.FromResult(ToFailure(new DomainValidationException("Select a car model to delete.", "car_model_selection_required"), "deleting Car model"));
            }

            return DeleteAsync($"api/carmodels/{selected.Id}", "Car model");
        }

        public Task<ManagementOperationResult> SaveLocationAsync(LocationManagementViewModel feature)
        {
            if (string.IsNullOrWhiteSpace(feature.NewLocationName))
            {
                return Task.FromResult(ToFailure(new DomainValidationException("Location name is required.", "location_name_required"), "saving Location"));
            }

            if (feature.NewLocationShippingFees < 0)
            {
                return Task.FromResult(ToFailure(new DomainValidationException("Shipping fees cannot be negative.", "location_shipping_invalid"), "saving Location"));
            }

            if (string.IsNullOrWhiteSpace(feature.NewLocationShippingFeesCurrencyCode)
                || feature.NewLocationShippingFeesCurrencyCode.Trim().Length != 3)
            {
                return Task.FromResult(ToFailure(new DomainValidationException("Shipping fees currency is required.", "location_shipping_currency_required"), "saving Location"));
            }

            var payload = new CreateLocationRequest
            {
                Name = feature.NewLocationName.Trim(),
                ShippingFees = feature.NewLocationShippingFees,
                ShippingFeesCurrencyCode = feature.NewLocationShippingFeesCurrencyCode.Trim().ToUpperInvariant()
            };

            return SaveAsync(
                feature.SelectedLocation is { LocationId: > 0 },
                feature.SelectedLocation?.LocationId,
                "api/locations",
                payload,
                "Location");
        }

        public Task<ManagementOperationResult> DeleteLocationAsync(LocationDto? selected)
        {
            if (selected is not { LocationId: > 0 })
            {
                return Task.FromResult(ToFailure(new DomainValidationException("Select a location to delete.", "location_selection_required"), "deleting Location"));
            }

            return DeleteAsync($"api/locations/{selected.LocationId}", "Location");
        }

        public Task<ManagementOperationResult> SaveUsedCarAsync(CreateUsedCarRequest request, UsedCarEntry? selected)
        {
            if (request.CarModelId <= 0)
            {
                return Task.FromResult(ToFailure(new DomainValidationException("Car model is required.", "used_car_model_required"), "saving Used car"));
            }

            if (request.ModelYear <= 0)
            {
                return Task.FromResult(ToFailure(new DomainValidationException("Model year is required.", "used_car_model_year_required"), "saving Used car"));
            }

            if (request.Price <= 0)
            {
                return Task.FromResult(ToFailure(new DomainValidationException("Price must be greater than zero.", "used_car_price_invalid"), "saving Used car"));
            }

            if (request.LocationId <= 0)
            {
                return Task.FromResult(ToFailure(new DomainValidationException("Location is required.", "used_car_location_required"), "saving Used car"));
            }

            if (request.Shipping < 0 || request.Customs < 0)
            {
                return Task.FromResult(ToFailure(new DomainValidationException("Expense values cannot be negative.", "used_car_expenses_invalid"), "saving Used car"));
            }

            if (request.IsReceived && request.Customs <= 0)
            {
                return Task.FromResult(ToFailure(new DomainValidationException("Customs should be different than 0 when the car is marked as received.", "used_car_customs_required_when_received"), "saving Used car"));
            }

            return SaveAsync(
                selected is { Id: > 0 },
                selected?.Id,
                "api/usedcars",
                request,
                "Used car");
        }

        public Task<ManagementOperationResult> DeleteUsedCarAsync(UsedCarEntry? selected)
        {
            if (selected is not { Id: > 0 })
            {
                return Task.FromResult(ToFailure(new DomainValidationException("Select a used car row to delete.", "used_car_selection_required"), "deleting Used car"));
            }

            return DeleteAsync($"api/usedcars/{selected.Id}", "Used car");
        }

        public Task<List<UsedCarImageDto>> GetUsedCarImagesAsync(int usedCarId)
            => _carCatalogApi.GetUsedCarImagesAsync(usedCarId);

        public Task UploadUsedCarImageAsync(int usedCarId, string filePath)
            => _carCatalogApi.UploadUsedCarImageAsync(usedCarId, filePath);

        public Task DeleteUsedCarImageAsync(int imageId)
            => _carCatalogApi.DeleteUsedCarImageAsync(imageId);

        public Task<ManagementOperationResult> SaveTransactionTypeAsync(TransactionTypeManagementViewModel feature)
        {
            if (string.IsNullOrWhiteSpace(feature.NewTransactionTypeName))
            {
                return Task.FromResult(ToFailure(new DomainValidationException("Transaction type name is required.", "transaction_type_required"), "saving Transaction type"));
            }

            if (feature.NewTransactionCounterRate <= 0)
            {
                return Task.FromResult(ToFailure(new DomainValidationException("Counter rate must be greater than zero.", "transaction_counter_rate_invalid"), "saving Transaction type"));
            }

            var payload = new CreateTransactionTypeRequest
            {
                Name = feature.NewTransactionTypeName.Trim(),
                CurrencyCode = (feature.NewTransactionCurrencyCode ?? string.Empty).Trim().ToUpperInvariant(),
                CounterRate = feature.NewTransactionCounterRate,
                IsActive = feature.NewTransactionIsActive
            };

            return SaveAsync(
                feature.SelectedTransactionType is { Id: > 0 },
                feature.SelectedTransactionType?.Id,
                "api/transactiontypes",
                payload,
                "Transaction type");
        }

        public Task<ManagementOperationResult> DeleteTransactionTypeAsync(TransactionTypeDto? selected)
        {
            if (selected is not { Id: > 0 })
            {
                return Task.FromResult(ToFailure(new DomainValidationException("Select a transaction type to delete.", "transaction_type_selection_required"), "deleting Transaction type"));
            }

            return DeleteAsync($"api/transactiontypes/{selected.Id}", "Transaction type");
        }

        private async Task<ManagementOperationResult> SaveAsync(bool isEditing, int? selectedId, string baseUrl, object payload, string entityName)
        {
            try
            {
                if (isEditing && selectedId is > 0)
                {
                    await _crudApi.PutAsync($"{baseUrl}/{selectedId}", payload);
                    return Success($"✓ {entityName} updated.");
                }

                await _crudApi.PostAsync(baseUrl, payload);
                return Success($"✓ {entityName} saved.");
            }
            catch (Exception ex)
            {
                return ToFailure(ex, $"saving {entityName}");
            }
        }

        private async Task<ManagementOperationResult> DeleteAsync(string url, string entityName)
        {
            try
            {
                await _crudApi.DeleteAsync(url);
                return Success($"✓ {entityName} deleted.");
            }
            catch (Exception ex)
            {
                return ToFailure(ex, $"deleting {entityName}");
            }
        }

        private static ManagementOperationResult Success(string message) => new() { Success = true, Message = message };
        private static ManagementOperationResult Fail(string message) => new() { Success = false, Message = message };
        private static ManagementOperationResult ToFailure(Exception exception, string operationName)
            => exception switch
            {
                DomainValidationException validation => Fail($"✗ {validation.Message}"),
                ApiClientException apiException => Fail($"✗ API error ({apiException.Code}): {apiException.Message}"),
                _ => Fail($"✗ Unexpected error while {operationName}.")
            };
    }
}
