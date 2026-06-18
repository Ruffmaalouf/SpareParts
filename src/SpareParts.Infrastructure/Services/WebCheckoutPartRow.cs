namespace SpareParts.Infrastructure.Services;

internal sealed class WebCheckoutPartRow
{
    public int Id { get; set; }
    public decimal SalePrice { get; set; }
    public string Currency { get; set; } = "USD";
    public decimal CostPrice { get; set; }
    public int AvailableQuantity { get; set; }
}
