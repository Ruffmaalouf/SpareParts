using SpareParts.Domain.Accounting;
using SpareParts.Domain.BusinessPartners;
using SpareParts.Domain.Cars;
using SpareParts.Domain.Inventory;
using SpareParts.Domain.MasterData;
using System;
using System.Collections.Generic;

namespace SpareParts.Desktop.Wpf.Management
{
    public sealed class ExcelImportExecutionContext
    {
        public IReadOnlyCollection<CustomerDto> Customers { get; init; } = Array.Empty<CustomerDto>();
        public IReadOnlyCollection<SupplierDto> Suppliers { get; init; } = Array.Empty<SupplierDto>();
        public IReadOnlyCollection<BrandDto> Brands { get; init; } = Array.Empty<BrandDto>();
        public IReadOnlyCollection<CategoryDto> Categories { get; init; } = Array.Empty<CategoryDto>();
        public IReadOnlyCollection<CarBrandDto> CarBrands { get; init; } = Array.Empty<CarBrandDto>();
        public IReadOnlyCollection<CarModelDto> CarModels { get; init; } = Array.Empty<CarModelDto>();
        public IReadOnlyCollection<LocationDto> Locations { get; init; } = Array.Empty<LocationDto>();
        public IReadOnlyCollection<WarehouseDto> Warehouses { get; init; } = Array.Empty<WarehouseDto>();
        public IReadOnlyCollection<TransactionTypeDto> TransactionTypes { get; init; } = Array.Empty<TransactionTypeDto>();
        public IReadOnlyCollection<UsedCarEntry> UsedCars { get; init; } = Array.Empty<UsedCarEntry>();
        public IReadOnlyCollection<AccountDto> Accounts { get; init; } = Array.Empty<AccountDto>();
        public IReadOnlyCollection<AccountTypeDefinitionDto> AccountTypes { get; init; } = Array.Empty<AccountTypeDefinitionDto>();
        public string BaseCurrencyCode { get; init; } = "USD";
        public string CounterCurrencyCode { get; init; } = "USD";
        public decimal DefaultCounterRate { get; init; } = 1m;
    }
}
