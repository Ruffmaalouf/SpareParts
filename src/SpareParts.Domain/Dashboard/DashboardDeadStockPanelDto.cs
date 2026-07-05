namespace SpareParts.Domain.Dashboard
{
    /// <summary>Dead-stock insight panel aggregated from the profit heatmap segments.</summary>
    public sealed class DashboardDeadStockPanelDto
    {
        public decimal TotalValue { get; set; }
        public decimal TotalUnits { get; set; }
        public List<DashboardDeadStockSegmentDto> TopSegments { get; set; } = new();
    }
}
