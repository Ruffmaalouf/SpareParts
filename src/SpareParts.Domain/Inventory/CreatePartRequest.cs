using System.ComponentModel.DataAnnotations;

namespace SpareParts.Domain.Inventory
{
    public sealed class CreatePartRequest
    {
        [Required, MaxLength(50)]
        public string InternalCode { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Barcode { get; set; }

        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? OEMNumber { get; set; }

        public PartCondition Condition { get; set; } = PartCondition.New;

        [Range(1, int.MaxValue)]
        public int CategoryId { get; set; }

        public int? BrandId { get; set; }

        [Range(0, double.MaxValue)]
        public decimal CostPrice { get; set; }

        [Range(0, double.MaxValue)]
        public decimal SalePrice { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? AveragePrice { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? EstimatedMarketPrice { get; set; }

        [Range(0, 100)]
        public decimal CostAllocationPercent { get; set; }

        [Range(0, double.MaxValue)]
        public decimal AllocatedCost { get; set; }

        [Range(0, double.MaxValue)]
        public decimal MinimumSellPrice { get; set; }

        [Range(0, double.MaxValue)]
        public decimal FastSalePrice { get; set; }

        [Range(0, double.MaxValue)]
        public decimal WholesalePrice { get; set; }

        [Range(0, double.MaxValue)]
        public decimal RecommendedPrice { get; set; }

        [MaxLength(50)]
        public string PricingStatus { get; set; } = PartPricingStatus.Manual;

        public DateTime? PricingCalculatedAt { get; set; }

        [Required, MaxLength(10)]
        public string Currency { get; set; } = PartDefaults.Currency;

        [Range(0, int.MaxValue)]
        public int MinStock { get; set; }

        [MaxLength(2000)]
        public string? Notes { get; set; }

        public int? UsedCarId { get; set; }
    }
}
