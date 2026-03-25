using SpareParts.Domain.BusinessPartners;
using SpareParts.Domain.Cars;
using SpareParts.Domain.Inventory;
using System.Collections.Generic;

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
}
