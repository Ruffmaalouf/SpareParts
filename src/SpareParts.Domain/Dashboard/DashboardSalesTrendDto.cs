namespace SpareParts.Domain.Dashboard
{
    /// <summary>Daily sales trend series for the dashboard trend chart (Report 04 §03/§07).</summary>
    public sealed class DashboardSalesTrendDto
    {
        public string Granularity { get; set; } = "daily";
        public int RangeDays { get; set; }
        public List<DashboardTrendPointDto> Series { get; set; } = new();
        public DateTime? PeakDate { get; set; }
        public decimal? PeakAmount { get; set; }
    }
}
