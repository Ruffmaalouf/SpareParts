namespace SpareParts.Infrastructure.Services
{
    /// <summary>Dapper row for the dashboard sales-trend query.</summary>
    public sealed class DashboardTrendRow
    {
        public DateTime SaleDate { get; set; }
        public decimal Amount { get; set; }
    }
}
