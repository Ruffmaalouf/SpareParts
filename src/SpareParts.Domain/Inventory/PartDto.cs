namespace SpareParts.Domain.Inventory
{
    public class PartDto
    {
        public int Id { get; set; }
        public string InternalCode { get; set; } = string.Empty;
        public string? Barcode { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? OEMNumber { get; set; }
        public PartCondition Condition { get; set; } = PartCondition.New;
        public int CategoryId { get; set; }
        public int? BrandId { get; set; }
        public decimal CostPrice { get; set; }
        public decimal SalePrice { get; set; }
        public decimal? AveragePrice { get; set; }
        public decimal? EstimatedMarketPrice { get; set; }
        public decimal CostAllocationPercent { get; set; }
        public decimal AllocatedCost { get; set; }
        public decimal MinimumSellPrice { get; set; }
        public decimal FastSalePrice { get; set; }
        public decimal WholesalePrice { get; set; }
        public decimal RecommendedPrice { get; set; }
        public string PricingStatus { get; set; } = "Manual";
        public DateTime? PricingCalculatedAt { get; set; }
        public string Currency { get; set; } = "USD";
        public int MinStock { get; set; }
        public int StockQuantity { get; set; }
        public int ReservedQuantity { get; set; }
        public int AvailableQuantity { get; set; }
        public string? ImageUrls { get; set; }
        public string? ImageUrl { get; set; }
        public string? Notes { get; set; }
        public int? UsedCarId { get; set; }
        public bool IsActive { get; set; }
    }
}
