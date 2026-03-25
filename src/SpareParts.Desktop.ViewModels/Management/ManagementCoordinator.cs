using SpareParts.Domain.BusinessPartners;
using SpareParts.Domain.Cars;
using SpareParts.Domain.Inventory;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SpareParts.Desktop.Wpf.Management
{
    public sealed class ManagementLoadResult
    {
        public required IEnumerable<CustomerDto> Customers { get; init; }
        public required IEnumerable<SupplierDto> Suppliers { get; init; }
        public required IEnumerable<BrandDto> Brands { get; init; }
        public required IEnumerable<CarBrandDto> CarBrands { get; init; }
        public required IEnumerable<CategoryDto> Categories { get; init; }
        public required IEnumerable<PartDto> Parts { get; init; }
        public required IEnumerable<CarModelDto> CarModels { get; init; }
    }

    public sealed class ManagementOperationResult
    {
        public bool Success { get; init; }
        public required string Message { get; init; }
    }

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
            await rolesVm.LoadAsync();

            return new ManagementLoadResult
            {
                Customers = customers,
                Suppliers = suppliers,
                Brands = brands,
                CarBrands = carBrands,
                Categories = categories,
                Parts = parts,
                CarModels = carModels
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

        public Task<ManagementOperationResult> SaveCarModelAsync(CarModelManagementViewModel feature)
        {
            if (string.IsNullOrWhiteSpace(feature.NewCarModelName))
            {
                return Task.FromResult(ToFailure(new DomainValidationException("Car model name is required.", "car_model_name_required"), "saving Car Model"));
            }

            var payload = new CreateCarModelRequest
            {
                Name = feature.NewCarModelName,
                Year = feature.NewCarModelYear,
                EngineType = feature.NewCarModelEngine,
                BasePrice = feature.NewCarModelBasePrice,
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
