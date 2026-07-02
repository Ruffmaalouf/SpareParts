using SpareParts.Domain.Inventory;

namespace SpareParts.Infrastructure.Services;

/// <summary>Raw dormant-stock projection used by <see cref="PartsService.GetDeadStock"/> to build the dead-stock report.</summary>
internal sealed class DeadStockQueryRow
{
    public int PartId { get; set; }
    public string InternalCode { get; set; } = string.Empty;
    public string PartName { get; set; } = string.Empty;
    public string? OemNumber { get; set; }
    public string Currency { get; set; } = PartDefaults.Currency;
    public decimal SalePrice { get; set; }
    public decimal UnitCost { get; set; }
    public decimal OnHand { get; set; }
    public decimal AvailableQuantity { get; set; }
    public decimal StockValue { get; set; }
    public decimal SoldQuantityLast90 { get; set; }
    public decimal SoldQuantityAllTime { get; set; }
    public DateTime? LastSoldAt { get; set; }
    public DateTime? LastReceivedAt { get; set; }
    public DateTime DormantSince { get; set; }
    public int DormantDays { get; set; }
}
