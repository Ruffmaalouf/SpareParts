using System;
using System.Collections.Generic;

namespace SpareParts.Domain.Inventory
{
    public sealed class DeadStockReportDto
    {
        public DateTime GeneratedAt { get; set; }
        public int MinDormantDays { get; set; }
        public int TotalCandidates { get; set; }
        public decimal TotalStockValue { get; set; }
        public string DominantCurrency { get; set; } = "USD";
        public List<DeadStockItemDto> Items { get; set; } = new();
    }

    public sealed class DeadStockItemDto
    {
        public int PartId { get; set; }
        public string InternalCode { get; set; } = string.Empty;
        public string PartName { get; set; } = string.Empty;
        public string? OemNumber { get; set; }
        public string Currency { get; set; } = "USD";
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
        public string PrimaryAction { get; set; } = string.Empty;
        public List<DeadStockActionDto> SuggestedActions { get; set; } = new();
    }

    public sealed class DeadStockActionDto
    {
        public string Key { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string Detail { get; set; } = string.Empty;
        public string Tone { get; set; } = "Neutral";
    }
}
