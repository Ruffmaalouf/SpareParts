namespace SpareParts.Infrastructure.Services;

/// <summary>Raw part inventory value projection used by <see cref="UsedCarsService"/> when computing a used car's remaining stock value.</summary>
internal sealed class UsedCarPartInventoryValueRow
{
    public int PartId { get; set; }
    public int UsedCarId { get; set; }
    public string? Currency { get; set; }
    public decimal UnitCost { get; set; }
    public decimal Quantity { get; set; }
}
