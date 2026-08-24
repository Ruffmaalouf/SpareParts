namespace SpareParts.Domain.Dashboard
{
    /// <summary>Low-stock insight panel: count plus the most urgent items (Report 04 §03).</summary>
    public sealed class DashboardLowStockPanelDto
    {
        public int TotalCount { get; set; }
        public List<DashboardLowStockItemDto> TopItems { get; set; } = new();
    }
}
