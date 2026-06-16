namespace SpareParts.Infrastructure.Services;

internal sealed class WhatsAppPartRow
{
    public string Name { get; set; } = string.Empty;
    public string InternalCode { get; set; } = string.Empty;
    public decimal SalePrice { get; set; }
    public string Currency { get; set; } = "USD";
}
