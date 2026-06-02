using System.ComponentModel.DataAnnotations;

namespace SpareParts.Domain.Inventory;

public class PartExpiryAlertDto
{
    public int PartId { get; set; }
    public string PartName { get; set; } = string.Empty;
    public string PartCode { get; set; } = string.Empty;
    public DateTime ExpiryDate { get; set; }
    public int DaysUntilExpiry { get; set; }
    public int StockQuantity { get; set; }
    public string ExpiryStatus { get; set; } = string.Empty;
}

public class SetPartExpiryRequest
{
    public DateTime? ExpiryDate { get; set; }
    [Range(0, 3650)] public int? ShelfLifeDays { get; set; }
}
