namespace SpareParts.Domain.OwnerCockpit
{
    public sealed class OwnerCockpitProfitHeatmapRowDto
    {
        public int? CategoryId { get; set; }
        public string SegmentKey { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        public decimal Cost { get; set; }
        public decimal Profit { get; set; }
        public decimal ProfitMarginPercent { get; set; }
        public decimal TurnoverUnits { get; set; }
        public decimal TurnoverRatePercent { get; set; }
        public decimal StockUnits { get; set; }
        public decimal StockValue { get; set; }
        public decimal DeadStockUnits { get; set; }
        public decimal DeadStockValue { get; set; }
        public decimal DeadStockPercent { get; set; }
        public int Score { get; set; }
        public string ProfitSignal { get; set; } = "Yellow";
        public string TurnoverSignal { get; set; } = "Yellow";
        public string DeadStockSignal { get; set; } = "Yellow";
        public string OverallSignal { get; set; } = "Yellow";
    }
}
