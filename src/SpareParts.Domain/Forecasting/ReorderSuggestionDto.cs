namespace SpareParts.Domain.Forecasting;

public class ReorderSuggestionDto
{
    public int PartId { get; set; }
    public string PartName { get; set; } = string.Empty;
    public string PartCode { get; set; } = string.Empty;
    public int CurrentStock { get; set; }
    public int ReorderPoint { get; set; }
    public int SuggestedOrderQuantity { get; set; }
    public int? PreferredSupplierId { get; set; }
    public string? PreferredSupplierName { get; set; }
    public decimal? LastPurchasePrice { get; set; }
    public int SalesLast30Days { get; set; }
    public int SalesLast90Days { get; set; }
}
