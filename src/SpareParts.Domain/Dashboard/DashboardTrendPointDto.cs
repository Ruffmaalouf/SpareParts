namespace SpareParts.Domain.Dashboard
{
    /// <summary>One day of the dashboard sales trend series.</summary>
    public sealed class DashboardTrendPointDto
    {
        public DateTime Date { get; set; }
        public decimal Amount { get; set; }
    }
}
