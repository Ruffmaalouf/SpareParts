namespace SpareParts.Infrastructure.Services;

internal sealed class PartPriceRow
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal SalePrice { get; set; }
    public string? Currency { get; set; }
}
