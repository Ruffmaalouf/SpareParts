namespace SpareParts.Desktop.Wpf.Interfaces
{
    /// <summary>Dead-stock panel segment of GET /api/dashboard/summary (Report 04 §03).</summary>
    public sealed class IgnitionDeadStockSegmentDto
    {
        public string SegmentKey { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public decimal DeadStockValue { get; set; }
        public int DeadStockUnits { get; set; }
    }
}
