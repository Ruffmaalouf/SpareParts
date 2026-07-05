namespace SpareParts.Domain.Dashboard
{
    /// <summary>One part at or below its minimum stock, with a reorder deep link.</summary>
    public sealed class DashboardLowStockItemDto
    {
        public int PartId { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal OnHand { get; set; }
        public decimal MinStock { get; set; }
        public string DeepLink { get; set; } = string.Empty;
    }
}
