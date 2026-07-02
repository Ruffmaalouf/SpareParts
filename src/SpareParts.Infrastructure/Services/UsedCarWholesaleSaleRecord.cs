namespace SpareParts.Infrastructure.Services;

/// <summary>Raw <c>dbo.UsedCarWholesaleSales</c> row used by <see cref="UsedCarsService"/>.</summary>
internal sealed class UsedCarWholesaleSaleRecord
{
    public int Id { get; set; }
    public string SaleNumber { get; set; } = string.Empty;
    public int UsedCarId { get; set; }
    public string UsedCar { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public int? CustomerId { get; set; }
    public string BuyerName { get; set; } = string.Empty;
    public string? BuyerPhone { get; set; }
    public DateTime SaleDate { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public decimal SalePrice { get; set; }
    public decimal SaleRateToBase { get; set; } = 1m;
    public decimal SalePriceBase { get; set; }
    public string CounterCurrencyCode { get; set; } = "USD";
    public decimal CounterRateToBase { get; set; } = 1m;
    public decimal SalePriceCounter { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal PaidBaseAmount { get; set; }
    public decimal PaidCounterAmount { get; set; }
    public string PaymentStatus { get; set; } = "Unpaid";
    public bool IsForParts { get; set; }
    public string? RepairItemsJson { get; set; }
    public decimal RepairTotalAmount { get; set; }
    public decimal RepairTotalBaseAmount { get; set; }
    public decimal RepairTotalCounterAmount { get; set; }
    public string? PaymentMethod { get; set; }
    public string? Notes { get; set; }
    public bool SoldAsIsAcknowledged { get; set; }
    public DateTime CreatedAt { get; set; }
    public int? CreatedByUserId { get; set; }
}
