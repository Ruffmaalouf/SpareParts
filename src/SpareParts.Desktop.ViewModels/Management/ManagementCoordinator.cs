using SpareParts.Domain.BusinessPartners;
using SpareParts.Domain.Cars;
using SpareParts.Domain.Inventory;
using SpareParts.Domain.MasterData;
using SpareParts.Desktop.Wpf.Interfaces;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace SpareParts.Desktop.Wpf.Management
{
    public class ManagementCoordinator
    {
        private readonly ICrudApiClient _crudApi;
        private readonly ICarCatalogApiClient _carCatalogApi;
        private readonly IPartsApiClient _partsApi;
        private readonly ExcelImportCoordinator _excelImportCoordinator;

        public ManagementCoordinator(
            ICrudApiClient crudApi,
            ICarCatalogApiClient carCatalogApi,
            IPartsApiClient partsApi)
        {
            _crudApi = crudApi;
            _carCatalogApi = carCatalogApi;
            _partsApi = partsApi;
            _excelImportCoordinator = new ExcelImportCoordinator(crudApi);
        }

        public Task<PartListingPackageDto> GetPartListingPackageAsync(int partId)
            => _partsApi.GetListingPackageAsync(partId);

        public Task<UsedCarListingPackageDto> GetUsedCarListingPackageAsync(int usedCarId)
            => _crudApi.GetAsync<UsedCarListingPackageDto>($"api/usedcars/{usedCarId}/listing-package");

        public async Task<ManagementLoadResult> LoadAllAsync(RolesViewModel rolesVm)
        {
            var customersTask = _crudApi.GetAllAsync<CustomerDto>("api/customers");
            var suppliersTask = _crudApi.GetAllAsync<SupplierDto>("api/suppliers");
            var brandsTask = _crudApi.GetAllAsync<BrandDto>("api/brands");
            var carBrandsTask = _carCatalogApi.GetCarBrandsAsync();
            var categoriesTask = _crudApi.GetAllAsync<CategoryDto>("api/categories");
            var partsTask = _crudApi.GetAllAsync<PartDto>("api/parts?page=1&pageSize=5000");
            var partRequestsTask = _crudApi.GetAllAsync<PartRequestDto>("api/partrequests");
            var carModelsTask = _crudApi.GetAllAsync<CarModelDto>("api/carmodels");
            var locationsTask = _crudApi.GetAllAsync<LocationDto>("api/locations");
            var usedCarsTask = _crudApi.GetAllAsync<UsedCarDto>("api/usedcars");
            var warehousesTask = _crudApi.GetAllAsync<WarehouseDto>("api/warehouses");
            var currencyRatesTask = _crudApi.GetAllAsync<CurrencyRateDto>("api/currencies");
            var transactionTypesTask = _crudApi.GetAllAsync<TransactionTypeDto>("api/transactiontypes");
            var appConstantsTask = _crudApi.GetAllAsync<AppConstantDto>("api/appconstants");
            var excelTargetsTask = _excelImportCoordinator.LoadTargetsAsync();
            var rolesTask = rolesVm.LoadAsync();

            await Task.WhenAll(
                customersTask,
                suppliersTask,
                brandsTask,
                carBrandsTask,
                categoriesTask,
                partsTask,
                partRequestsTask,
                carModelsTask,
                locationsTask,
                usedCarsTask,
                warehousesTask,
                currencyRatesTask,
                transactionTypesTask,
                appConstantsTask,
                excelTargetsTask,
                rolesTask).ConfigureAwait(false);

            return new ManagementLoadResult
            {
                Customers = customersTask.Result,
                Suppliers = suppliersTask.Result,
                Brands = brandsTask.Result,
                CarBrands = carBrandsTask.Result,
                Categories = categoriesTask.Result,
                Parts = partsTask.Result,
                PartRequests = partRequestsTask.Result,
                CarModels = carModelsTask.Result,
                Locations = locationsTask.Result,
                UsedCars = usedCarsTask.Result,
                Warehouses = warehousesTask.Result,
                CurrencyRates = currencyRatesTask.Result,
                TransactionTypes = transactionTypesTask.Result,
                AppConstants = appConstantsTask.Result
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

        public async Task<ManagementOperationResult> SavePartAsync(PartManagementViewModel feature)
        {
            if (string.IsNullOrWhiteSpace(feature.NewPartCode) || string.IsNullOrWhiteSpace(feature.NewPartName))
            {
                return ToFailure(new DomainValidationException("Part code and name are required.", "part_required_fields_missing"), "saving Part");
            }

            decimal? averagePrice;
            decimal? estimatedMarketPrice;
            try
            {
                averagePrice = ParseOptionalDecimal(feature.NewPartAveragePrice, "Average price", "part_average_price_invalid");
                estimatedMarketPrice = ParseOptionalDecimal(feature.NewPartEstimatedMarketPrice, "Estimated market price", "part_estimated_market_price_invalid");
            }
            catch (DomainValidationException exception)
            {
                return ToFailure(exception, "saving Part");
            }

            var payload = new CreatePartRequest
            {
                InternalCode = feature.NewPartCode,
                Barcode = NormalizeOptional(feature.NewPartBarcode),
                Name = feature.NewPartName,
                OEMNumber = feature.NewPartOEM,
                Condition = feature.SelectedPart?.Condition ?? PartCondition.New,
                CategoryId = feature.NewPartCategoryId,
                BrandId = feature.NewPartBrandId,
                CostPrice = feature.NewPartCostPrice,
                SalePrice = feature.NewPartSalePrice,
                AveragePrice = averagePrice,
                EstimatedMarketPrice = estimatedMarketPrice,
                CostAllocationPercent = feature.SelectedPart?.CostAllocationPercent ?? 0m,
                AllocatedCost = feature.SelectedPart?.AllocatedCost ?? 0m,
                MinimumSellPrice = feature.SelectedPart?.MinimumSellPrice ?? 0m,
                FastSalePrice = feature.SelectedPart?.FastSalePrice ?? 0m,
                WholesalePrice = feature.SelectedPart?.WholesalePrice ?? 0m,
                RecommendedPrice = feature.SelectedPart?.RecommendedPrice ?? 0m,
                PricingStatus = feature.SelectedPart?.PricingStatus ?? "Manual",
                PricingCalculatedAt = feature.SelectedPart?.PricingCalculatedAt,
                Currency = feature.NewPartCurrency,
                MinStock = feature.NewPartMinStock,
                Notes = feature.NewPartNotes,
                UsedCarId = feature.SelectedPart?.UsedCarId
            };

            var isEditing = feature.SelectedPart is { Id: > 0 };
            var result = await SaveAsync(
                isEditing,
                feature.SelectedPart?.Id,
                "api/parts",
                payload,
                "Part");

            if (!result.Success || isEditing)
            {
                return result;
            }

            return Success($"Part added: {payload.Name} - {FormatCurrency(payload.SalePrice, payload.Currency)}.");
        }

        public Task<ManagementOperationResult> DeletePartAsync(PartDto? selected)
        {
            if (selected is not { Id: > 0 })
            {
                return Task.FromResult(ToFailure(new DomainValidationException("Select a part to delete.", "part_selection_required"), "deleting Part"));
            }

            return DeleteAsync($"api/parts/{selected.Id}", "Part");
        }

        public async Task<ManagementOperationResult> SavePartRequestAsync(PartRequestsManagementViewModel feature)
        {
            if (string.IsNullOrWhiteSpace(feature.NewRequestCustomerName))
            {
                return ToFailure(new DomainValidationException("Customer name is required.", "part_request_customer_required"), "saving part request");
            }

            if (string.IsNullOrWhiteSpace(feature.NewRequestPartName))
            {
                return ToFailure(new DomainValidationException("Requested part name is required.", "part_request_name_required"), "saving part request");
            }

            if (feature.ReserveOnCreate && feature.NewRequestPartId is null)
            {
                return ToFailure(new DomainValidationException("Match a catalog part before starting a reservation clock.", "part_request_reservation_part_required"), "saving part request");
            }

            var payload = new CreatePartRequestItemRequest
            {
                PartId = feature.NewRequestPartId,
                CustomerId = feature.NewRequestCustomerId,
                CustomerName = feature.NewRequestCustomerName.Trim(),
                CustomerPhone = NormalizeOptional(feature.NewRequestCustomerPhone),
                RequestedPartName = feature.NewRequestPartName.Trim(),
                RequestedOemNumber = NormalizeOptional(feature.NewRequestOem),
                VehicleDetails = NormalizeOptional(feature.NewRequestVehicleDetails),
                Quantity = Math.Max(1, feature.NewRequestQuantity),
                Notes = NormalizeOptional(feature.NewRequestNotes)
            };

            try
            {
                var requestId = await _crudApi.PostAsync<int>("api/partrequests", payload);
                if (feature.ReserveOnCreate)
                {
                    await ReservePartRequestCoreAsync(
                        requestId,
                        Math.Max(1, feature.NewRequestQuantity),
                        feature.ReservationExpiresAt,
                        feature.ReservationExpirationAction);
                    return Success("✓ Part request saved and reserved.");
                }

                return Success("✓ Part request saved.");
            }
            catch (Exception ex)
            {
                return ToFailure(ex, "saving part request");
            }
        }

        public async Task<ManagementOperationResult> ReservePartRequestAsync(
            PartRequestDto? selected,
            DateTime expiresAt,
            string expirationAction)
        {
            if (selected is not { Id: > 0 })
            {
                return ToFailure(new DomainValidationException("Select a part request first.", "part_request_selection_required"), "reserving part request");
            }

            if (selected.PartId is null)
            {
                return ToFailure(new DomainValidationException("Match a catalog part before starting a reservation clock.", "part_request_reservation_part_required"), "reserving part request");
            }

            try
            {
                var reservation = await ReservePartRequestCoreAsync(
                    selected.Id,
                    Math.Max(1, selected.Quantity),
                    expiresAt,
                    expirationAction);
                return Success($"✓ Reserved {reservation.Quantity:N0} item(s) until {reservation.ExpiresAt.ToLocalTime():yyyy-MM-dd HH:mm}.");
            }
            catch (Exception ex)
            {
                return ToFailure(ex, "reserving part request");
            }
        }

        public async Task<ManagementOperationResult> ReleasePartRequestReservationAsync(PartRequestDto? selected)
        {
            if (selected is not { Id: > 0 })
            {
                return ToFailure(new DomainValidationException("Select a part request first.", "part_request_selection_required"), "releasing reservation");
            }

            try
            {
                await _crudApi.PostAsync(
                    $"api/partrequests/{selected.Id}/release-reservation",
                    new ReleasePartRequestReservationRequest { Reason = "Released from WPF" });
                return Success("✓ Reservation released.");
            }
            catch (Exception ex)
            {
                return ToFailure(ex, "releasing reservation");
            }
        }

        public Task<ManagementOperationResult> UpdatePartRequestStatusAsync(PartRequestDto? selected, string status)
        {
            if (selected is not { Id: > 0 })
            {
                return Task.FromResult(ToFailure(new DomainValidationException("Select a part request first.", "part_request_selection_required"), "updating part request"));
            }

            return UpdatePartRequestStatusCoreAsync(selected.Id, status);
        }

        public Task<ManagementOperationResult> DeletePartRequestAsync(PartRequestDto? selected)
        {
            if (selected is not { Id: > 0 })
            {
                return Task.FromResult(ToFailure(new DomainValidationException("Select a part request to delete.", "part_request_selection_required"), "deleting part request"));
            }

            return DeleteAsync($"api/partrequests/{selected.Id}", "Part request");
        }

        private async Task<ManagementOperationResult> UpdatePartRequestStatusCoreAsync(int id, string status)
        {
            try
            {
                await _crudApi.PutAsync(
                    $"api/partrequests/{id}/status",
                    new UpdatePartRequestStatusRequest { Status = status });
                return Success("✓ Part request updated.");
            }
            catch (Exception ex)
            {
                return ToFailure(ex, "updating part request");
            }
        }

        private Task<PartRequestReservationDto> ReservePartRequestCoreAsync(
            int id,
            int quantity,
            DateTime expiresAt,
            string expirationAction)
            => _crudApi.PostAsync<PartRequestReservationDto>(
                $"api/partrequests/{id}/reserve",
                new ReservePartRequestRequest
                {
                    Quantity = Math.Max(1, quantity),
                    ExpiresAt = expiresAt,
                    ExpirationAction = PartReservationExpirationAction.Normalize(expirationAction)
                });

        public Task<List<PartDto>> GetAllPartsAsync()
            => _crudApi.GetAllAsync<PartDto>("api/parts?page=1&pageSize=5000");

        public IReadOnlyList<ExcelImportTargetOption> GetExcelImportTargets()
            => _excelImportCoordinator.GetTargets();

        public Task LoadExcelImportTargetsAsync()
            => _excelImportCoordinator.LoadTargetsAsync();

        public IReadOnlyList<ExcelImportFieldDefinition> GetExcelImportFields(string targetKey)
            => _excelImportCoordinator.GetFields(targetKey);

        public ExcelWorkbookSheet ReadExcelWorkbook(string filePath)
            => _excelImportCoordinator.ReadWorkbook(filePath);

        public IReadOnlyDictionary<string, string?> ResolveExcelImportMappings(
            string targetKey,
            IReadOnlyList<string> workbookHeaders,
            IEnumerable<AppConstantDto>? appConstants,
            IReadOnlyDictionary<string, string?>? overrides = null)
            => _excelImportCoordinator.ResolveMappings(targetKey, workbookHeaders, appConstants, overrides);

        public Task SaveExcelImportProfileAsync(string targetKey, IReadOnlyDictionary<string, string?> mappings)
            => _excelImportCoordinator.SaveProfileAsync(targetKey, mappings);

        public Task<ExcelImportResult> ImportFromExcelAsync(
            string targetKey,
            string filePath,
            ExcelImportExecutionContext context,
            IReadOnlyDictionary<string, string?>? mappings,
            IReadOnlyCollection<AppConstantDto>? appConstants)
            => _excelImportCoordinator.ImportAsync(targetKey, filePath, context, mappings, appConstants);

        public async Task<PartExcelImportResult> ImportPartsFromExcelAsync(string filePath)
        {
            var categories = await _crudApi.GetAllAsync<CategoryDto>("api/categories");
            var brands = await _crudApi.GetAllAsync<BrandDto>("api/brands");

            return await ImportPartsFromExcelAsync(filePath, categories, brands);
        }

        public async Task<PartExcelImportResult> ImportPartsFromExcelAsync(
            string filePath,
            IReadOnlyCollection<CategoryDto> categories,
            IReadOnlyCollection<BrandDto> brands)
        {
            var appConstants = await _crudApi.GetAllAsync<AppConstantDto>("api/appconstants");
            await _excelImportCoordinator.LoadTargetsAsync();
            var partsTargetKey = _excelImportCoordinator.GetTargetKeyForTable("Parts") ?? "dbo.Parts";
            var importResult = await _excelImportCoordinator.ImportAsync(
                partsTargetKey,
                filePath,
                new ExcelImportExecutionContext
                {
                    Categories = categories,
                    Brands = brands,
                    BaseCurrencyCode = ResolveCurrencyCode(appConstants, "BaseCurrencyCode")
                        ?? ResolveCurrencyCode(appConstants, "DefaultCurrencyCode")
                        ?? "USD",
                    CounterCurrencyCode = ResolveCurrencyCode(appConstants, "CounterCurrencyCode")
                        ?? ResolveCurrencyCode(appConstants, "BaseCurrencyCode")
                        ?? "USD",
                    DefaultCounterRate = ResolveDefaultCounterRate(appConstants)
                },
                mappings: null,
                appConstants: appConstants);

            return ToPartExcelImportResult(importResult);
        }

        public Task SetPartUsedCarAsync(int partId, int? usedCarId)
            => _crudApi.PutAsync(
                $"api/parts/{partId}/usedcar",
                new UpdatePartUsedCarRequest
                {
                    UsedCarId = usedCarId
                });

        public async Task<ManagementOperationResult> GeneratePartNotesAsync(
            PartManagementViewModel feature,
            IReadOnlyDictionary<int, string> categoryLookup,
            IReadOnlyDictionary<int, string> brandLookup)
        {
            if (string.IsNullOrWhiteSpace(feature.NewPartCode) || string.IsNullOrWhiteSpace(feature.NewPartName))
            {
                return ToFailure(new DomainValidationException("Part code and name are required before using AI notes.", "part_ai_required_fields_missing"), "generating AI part notes");
            }

            try
            {
                var response = await _partsApi.GeneratePartNotesAsync(new GeneratePartNotesRequest
                {
                    InternalCode = feature.NewPartCode.Trim(),
                    Name = feature.NewPartName.Trim(),
                    OEMNumber = NormalizeOptional(feature.NewPartOEM),
                    CategoryId = feature.NewPartCategoryId,
                    CategoryName = categoryLookup.TryGetValue(feature.NewPartCategoryId, out var categoryName) ? categoryName : null,
                    BrandId = feature.NewPartBrandId,
                    BrandName = feature.NewPartBrandId is int brandId && brandLookup.TryGetValue(brandId, out var brandName) ? brandName : null,
                    CostPrice = feature.NewPartCostPrice,
                    SalePrice = feature.NewPartSalePrice,
                    Currency = string.IsNullOrWhiteSpace(feature.NewPartCurrency) ? "USD" : feature.NewPartCurrency.Trim().ToUpperInvariant(),
                    MinStock = feature.NewPartMinStock,
                    ExistingNotes = NormalizeOptional(feature.NewPartNotes)
                });

                feature.NewPartNotes = response.SuggestedNotes;
                feature.NewPartAveragePrice = response.SuggestedAveragePrice?.ToString("0.##") ?? feature.NewPartAveragePrice;

                return response.SuggestedAveragePrice.HasValue
                    ? Success("✓ AI draft updated notes and average price.")
                    : Success("✓ AI draft added to part notes.");
            }
            catch (Exception ex)
            {
                return ToFailure(ex, "generating AI part notes");
            }
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

        public Task<ManagementOperationResult> SaveWarehouseAsync(WarehouseManagementViewModel feature)
        {
            if (string.IsNullOrWhiteSpace(feature.NewWarehouseName))
            {
                return Task.FromResult(ToFailure(new DomainValidationException("Warehouse name is required.", "warehouse_name_required"), "saving Warehouse"));
            }

            var payload = new CreateWarehouseRequest
            {
                Name = feature.NewWarehouseName.Trim(),
                Barcode = NormalizeOptional(feature.NewWarehouseBarcode),
                Address = string.IsNullOrWhiteSpace(feature.NewWarehouseAddress)
                    ? null
                    : feature.NewWarehouseAddress.Trim(),
                IsMain = feature.NewWarehouseIsMain
            };

            return SaveAsync(
                feature.SelectedWarehouse is { Id: > 0 },
                feature.SelectedWarehouse?.Id,
                "api/warehouses",
                payload,
                "Warehouse");
        }

        public Task<ManagementOperationResult> DeleteWarehouseAsync(WarehouseDto? selected)
        {
            if (selected is not { Id: > 0 })
            {
                return Task.FromResult(ToFailure(new DomainValidationException("Select a warehouse to delete.", "warehouse_selection_required"), "deleting Warehouse"));
            }

            return DeleteAsync($"api/warehouses/{selected.Id}", "Warehouse");
        }

        public Task<ManagementOperationResult> SaveUsedCarAsync(CreateUsedCarRequest request, UsedCarEntry? selected)
        {
            if (request.SupplierId <= 0)
            {
                return Task.FromResult(ToFailure(new DomainValidationException("Supplier is required.", "used_car_supplier_required"), "saving Used car"));
            }

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

            if (request.PartOut < 0 || request.Shipping < 0 || request.Customs < 0 || request.Repairs < 0)
            {
                return Task.FromResult(ToFailure(new DomainValidationException("Expense values cannot be negative.", "used_car_expenses_invalid"), "saving Used car"));
            }

            if (request.ExpectedSellThroughRate <= 0m || request.ExpectedSellThroughRate > 1m)
            {
                return Task.FromResult(ToFailure(new DomainValidationException("Expected sell-through rate must be between 0 and 1.", "used_car_sell_through_invalid"), "saving Used car"));
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

            if (string.IsNullOrWhiteSpace(feature.NewTransactionSerialNumberFormat))
            {
                return Task.FromResult(ToFailure(new DomainValidationException("Serial number format is required.", "transaction_serial_format_required"), "saving Transaction type"));
            }

            if (feature.NewTransactionStartNumber <= 0)
            {
                return Task.FromResult(ToFailure(new DomainValidationException("Start number must be greater than zero.", "transaction_start_number_invalid"), "saving Transaction type"));
            }

            if (feature.NewTransactionCurrentNumber < 0)
            {
                return Task.FromResult(ToFailure(new DomainValidationException("Current number cannot be negative.", "transaction_current_number_invalid"), "saving Transaction type"));
            }

            var payload = new CreateTransactionTypeRequest
            {
                Name = feature.NewTransactionTypeName.Trim(),
                CurrencyCode = (feature.NewTransactionCurrencyCode ?? string.Empty).Trim().ToUpperInvariant(),
                CounterRate = feature.NewTransactionCounterRate,
                SerialNumberFormat = feature.NewTransactionSerialNumberFormat.Trim(),
                StartNumber = feature.NewTransactionStartNumber,
                CurrentNumber = feature.NewTransactionCurrentNumber,
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
        private static string? NormalizeOptional(string? value)
            => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        private static string FormatCurrency(decimal value, string? currency)
            => $"{(string.IsNullOrWhiteSpace(currency) ? "USD" : currency.Trim())} {value.ToString("N2", CultureInfo.CurrentCulture)}";

        private static CreatePartRequest BuildImportedPartRequest(
            PartExcelImportRow row,
            int defaultCategoryId,
            IReadOnlyDictionary<int, CategoryDto> categoriesById,
            IReadOnlyDictionary<string, int> categoriesByName,
            IReadOnlyDictionary<int, BrandDto> brandsById,
            IReadOnlyDictionary<string, int> brandsByName)
        {
            var internalCode = RequireImportedValue(row.InternalCode, "Internal Code", row.RowNumber);
            var name = RequireImportedValue(row.Name, "Name", row.RowNumber);

            return new CreatePartRequest
            {
                InternalCode = internalCode.Trim(),
                Barcode = NormalizeOptional(row.Barcode),
                Name = name.Trim(),
                OEMNumber = NormalizeOptional(row.OEMNumber),
                Condition = ParseImportedCondition(row.Condition, row.RowNumber),
                CategoryId = ResolveImportedCategoryId(
                    row.Category,
                    defaultCategoryId,
                    categoriesById,
                    categoriesByName,
                    row.RowNumber),
                BrandId = ResolveImportedBrandId(
                    row.Brand,
                    brandsById,
                    brandsByName,
                    row.RowNumber),
                CostPrice = ParseImportedDecimal(row.CostPrice, "Cost Price", row.RowNumber, 0m),
                SalePrice = ParseImportedDecimal(row.SalePrice, "Sale Price", row.RowNumber, 0m),
                AveragePrice = ParseImportedOptionalDecimal(row.AveragePrice, "Average Price", row.RowNumber),
                EstimatedMarketPrice = ParseImportedOptionalDecimal(row.EstimatedMarketPrice, "Estimated Market Price", row.RowNumber),
                Currency = ParseImportedCurrency(row.Currency),
                MinStock = ParseImportedInteger(row.MinStock, "Min Stock", row.RowNumber, 0),
                Notes = NormalizeOptional(row.Notes)
            };
        }

        private static string RequireImportedValue(string? value, string fieldName, int rowNumber)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }

            throw new DomainValidationException(
                $"{fieldName} is required.",
                $"part_import_{NormalizeImportKey(fieldName)}_required_row_{rowNumber}");
        }

        private static int ResolveImportedCategoryId(
            string? rawValue,
            int defaultCategoryId,
            IReadOnlyDictionary<int, CategoryDto> categoriesById,
            IReadOnlyDictionary<string, int> categoriesByName,
            int rowNumber)
        {
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return defaultCategoryId;
            }

            var trimmed = rawValue.Trim();
            if (int.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out var categoryId))
            {
                if (categoriesById.ContainsKey(categoryId))
                {
                    return categoryId;
                }

                throw new DomainValidationException($"Category '{trimmed}' was not found.", $"part_import_category_missing_row_{rowNumber}");
            }

            var normalized = NormalizeImportKey(trimmed);
            if (categoriesByName.TryGetValue(normalized, out var resolvedCategoryId))
            {
                return resolvedCategoryId;
            }

            throw new DomainValidationException($"Category '{trimmed}' was not found.", $"part_import_category_missing_row_{rowNumber}");
        }

        private static int? ResolveImportedBrandId(
            string? rawValue,
            IReadOnlyDictionary<int, BrandDto> brandsById,
            IReadOnlyDictionary<string, int> brandsByName,
            int rowNumber)
        {
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return null;
            }

            var trimmed = rawValue.Trim();
            if (int.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out var brandId))
            {
                if (brandsById.ContainsKey(brandId))
                {
                    return brandId;
                }

                throw new DomainValidationException($"Brand '{trimmed}' was not found.", $"part_import_brand_missing_row_{rowNumber}");
            }

            var normalized = NormalizeImportKey(trimmed);
            if (brandsByName.TryGetValue(normalized, out var resolvedBrandId))
            {
                return resolvedBrandId;
            }

            throw new DomainValidationException($"Brand '{trimmed}' was not found.", $"part_import_brand_missing_row_{rowNumber}");
        }

        private static PartCondition ParseImportedCondition(string? rawValue, int rowNumber)
        {
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return PartCondition.New;
            }

            var normalized = NormalizeImportKey(rawValue);
            return normalized switch
            {
                "1" or "new" => PartCondition.New,
                "2" or "used" => PartCondition.Used,
                "3" or "rebuilt" => PartCondition.Rebuilt,
                _ => throw new DomainValidationException(
                    $"Condition '{rawValue}' is invalid. Use New, Used, or Rebuilt.",
                    $"part_import_condition_invalid_row_{rowNumber}")
            };
        }

        private static decimal ParseImportedDecimal(string? rawValue, string fieldName, int rowNumber, decimal defaultValue)
        {
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return defaultValue;
            }

            if (decimal.TryParse(rawValue, NumberStyles.Number, CultureInfo.CurrentCulture, out var currentCultureValue)
                || decimal.TryParse(rawValue, NumberStyles.Number, CultureInfo.InvariantCulture, out currentCultureValue))
            {
                return currentCultureValue;
            }

            throw new DomainValidationException(
                $"{fieldName} '{rawValue}' is not a valid number.",
                $"part_import_{NormalizeImportKey(fieldName)}_invalid_row_{rowNumber}");
        }

        private static decimal? ParseImportedOptionalDecimal(string? rawValue, string fieldName, int rowNumber)
        {
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return null;
            }

            return ParseImportedDecimal(rawValue, fieldName, rowNumber, 0m);
        }

        private static int ParseImportedInteger(string? rawValue, string fieldName, int rowNumber, int defaultValue)
        {
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return defaultValue;
            }

            if (int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.CurrentCulture, out var currentCultureValue)
                || int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out currentCultureValue))
            {
                return currentCultureValue;
            }

            throw new DomainValidationException(
                $"{fieldName} '{rawValue}' is not a valid whole number.",
                $"part_import_{NormalizeImportKey(fieldName)}_invalid_row_{rowNumber}");
        }

        private static string ParseImportedCurrency(string? rawValue)
        {
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return "USD";
            }

            return rawValue.Trim().ToUpperInvariant();
        }

        private static decimal? ParseOptionalDecimal(string? value, string fieldLabel, string errorCode)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.CurrentCulture, out var parsed)
                || decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out parsed))
            {
                return parsed;
            }

            throw new DomainValidationException($"{fieldLabel} must be a valid number.", errorCode);
        }

        private static string NormalizeImportKey(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            return new string(value
                .Where(char.IsLetterOrDigit)
                .Select(char.ToLowerInvariant)
                .ToArray());
        }

        private static string MapPartImportError(Exception exception)
            => exception switch
            {
                DomainValidationException validation => validation.Message,
                ApiClientException apiException => $"API error ({apiException.Code}): {apiException.Message}",
                _ when !string.IsNullOrWhiteSpace(exception.Message) => exception.Message,
                _ => "Unexpected error while importing parts."
            };

        private static PartExcelImportResult ToPartExcelImportResult(ExcelImportResult importResult)
        {
            var result = new PartExcelImportResult();
            for (var index = 0; index < importResult.ImportedCount; index++)
            {
                result.AddImported();
            }

            foreach (var error in importResult.Errors)
            {
                result.AddError(error);
            }

            return result;
        }

        private static string? ResolveCurrencyCode(IEnumerable<AppConstantDto> constants, string key)
        {
            var value = constants
                .FirstOrDefault(item => string.Equals(item.Key, key, StringComparison.OrdinalIgnoreCase))
                ?.Value;

            if (string.IsNullOrWhiteSpace(value) || value.Trim().Length != 3)
            {
                return null;
            }

            return value.Trim().ToUpperInvariant();
        }

        private static decimal ResolveDefaultCounterRate(IEnumerable<AppConstantDto> constants)
        {
            var value = constants
                .FirstOrDefault(item => string.Equals(item.Key, "DefaultCounterRate", StringComparison.OrdinalIgnoreCase))
                ?.Value;

            return decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsedRate)
                && parsedRate > 0
                    ? parsedRate
                    : 1m;
        }

        private static ManagementOperationResult ToFailure(Exception exception, string operationName)
            => exception switch
            {
                DomainValidationException validation => Fail($"✗ {validation.Message}"),
                ApiClientException apiException => Fail($"✗ API error ({apiException.Code}): {apiException.Message}"),
                _ => Fail($"✗ Unexpected error while {operationName}.")
            };
    }
}
