namespace SpareParts.Desktop.Wpf.Interfaces
{
    /// <summary>Low-stock panel row of GET /api/dashboard/summary (Report 04 §03).</summary>
    public sealed class IgnitionLowStockItemDto
    {
        public int PartId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int OnHand { get; set; }
        public int MinStock { get; set; }
        public string DeepLink { get; set; } = string.Empty;
    }
}
