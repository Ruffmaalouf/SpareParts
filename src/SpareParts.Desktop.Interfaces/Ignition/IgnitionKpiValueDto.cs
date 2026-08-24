namespace SpareParts.Desktop.Wpf.Interfaces
{
    /// <summary>Single KPI value with trend delta (GET /api/dashboard/summary, Report 04 §03).</summary>
    public sealed class IgnitionKpiValueDto
    {
        public decimal Value { get; set; }
        public decimal? DeltaPercent { get; set; }

        /// <summary>"up" | "down" | "flat".</summary>
        public string Direction { get; set; } = "flat";
    }
}
