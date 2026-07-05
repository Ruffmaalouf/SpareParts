namespace SpareParts.Domain.Dashboard
{
    /// <summary>One inventory segment's dead-stock exposure.</summary>
    public sealed class DashboardDeadStockSegmentDto
    {
        public string SegmentKey { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public decimal DeadStockValue { get; set; }
        public decimal DeadStockUnits { get; set; }
    }
}
