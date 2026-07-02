using SpareParts.Domain.Inventory;

namespace SpareParts.Infrastructure.Services;

/// <summary>Raw part + used-car projection used by <see cref="PartsService.BuildListingPackage"/> to build a marketplace listing package.</summary>
internal sealed class PartListingQueryRow
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string OEMNumber { get; set; } = string.Empty;
    public PartCondition Condition { get; set; }
    public decimal SalePrice { get; set; }
    public string Currency { get; set; } = string.Empty;
    public int? UsedCarId { get; set; }
    public string? UsedCarName { get; set; }
}
