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
                return Task.FromResult(Fail("✗ Customer name is required."));
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
                return Task.FromResult(Fail("✗ Select a customer to delete."));
            }

            return DeleteAsync($"api/customers/{selected.Id}", "Customer");
        }

        public Task<ManagementOperationResult> SaveSupplierAsync(SupplierManagementViewModel feature)
        {
            if (string.IsNullOrWhiteSpace(feature.NewSupplierName))
            {
                return Task.FromResult(Fail("✗ Supplier name is required."));
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
                return Task.FromResult(Fail("✗ Select a supplier to delete."));
            }

            return DeleteAsync($"api/suppliers/{selected.Id}", "Supplier");
        }

        public Task<ManagementOperationResult> SaveBrandAsync(BrandManagementViewModel feature)
        {
            if (string.IsNullOrWhiteSpace(feature.NewBrandName))
            {
                return Task.FromResult(Fail("✗ Brand name is required."));
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
                return Task.FromResult(Fail("✗ Select a brand to delete."));
            }

            return DeleteAsync($"api/brands/{selected.Id}", "Brand");
        }

        public Task<ManagementOperationResult> SavePartAsync(PartManagementViewModel feature)
        {
            if (string.IsNullOrWhiteSpace(feature.NewPartCode) || string.IsNullOrWhiteSpace(feature.NewPartName))
            {
                return Task.FromResult(Fail("✗ Part code and name are required."));
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
                return Task.FromResult(Fail("✗ Select a part to delete."));
            }

            return DeleteAsync($"api/parts/{selected.Id}", "Part");
        }

        public Task<ManagementOperationResult> SaveCarModelAsync(CarModelManagementViewModel feature)
        {
            if (string.IsNullOrWhiteSpace(feature.NewCarModelName))
            {
                return Task.FromResult(Fail("✗ Car model name is required."));
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
                return Task.FromResult(Fail("✗ Select a car model to delete."));
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
                return Fail($"✗ Error saving {entityName}: {ex.Message}");
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
                return Fail($"✗ Error deleting {entityName}: {ex.Message}");
            }
        }

        private static ManagementOperationResult Success(string message) => new() { Success = true, Message = message };
        private static ManagementOperationResult Fail(string message) => new() { Success = false, Message = message };
    }
}
