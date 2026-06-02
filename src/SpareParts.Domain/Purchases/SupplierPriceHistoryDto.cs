namespace SpareParts.Domain.Purchases;

public class SupplierPriceHistoryDto
{
    public int Id { get; set; }
    public int PartId { get; set; }
    public string PartName { get; set; } = string.Empty;
    public string PartCode { get; set; } = string.Empty;
    public int SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public string? CurrencyCode { get; set; }
    public int Quantity { get; set; }
    public int? InvoiceId { get; set; }
    public DateTime RecordedAt { get; set; }
}

public class SupplierPriceComparisonDto
{
    public int PartId { get; set; }
    public string PartName { get; set; } = string.Empty;
    public string PartCode { get; set; } = string.Empty;
    public List<SupplierLastPriceDto> SupplierPrices { get; set; } = new();
}

public class SupplierLastPriceDto
{
    public int SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public decimal LastPrice { get; set; }
    public decimal LowestPrice { get; set; }
    public decimal AveragePrice { get; set; }
    public int PurchaseCount { get; set; }
    public DateTime LastPurchaseDate { get; set; }
}

public class RecordSupplierPriceRequest
{
    public int PartId { get; set; }
    public int SupplierId { get; set; }
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; } = 1;
    public int? InvoiceId { get; set; }
    public string? CurrencyCode { get; set; }
}
