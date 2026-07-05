namespace SpareParts.Domain.Dashboard
{
    /// <summary>Single KPI tile value with an optional day-over-day delta.</summary>
    public sealed class DashboardKpiDto
    {
        public decimal Value { get; set; }
        public decimal? DeltaPercent { get; set; }

        /// <summary>"up", "down", or "flat".</summary>
        public string Direction { get; set; } = "flat";
    }
}
