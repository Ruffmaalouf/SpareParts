namespace SpareParts.Desktop.Wpf.Interfaces
{
    /// <summary>KPI block of GET /api/clients/{id}/workspace (Report 04 §05).</summary>
    public sealed class IgnitionClientKpisDto
    {
        public decimal LifetimeRevenue { get; set; }
        public int LifetimeInvoiceCount { get; set; }
        public int AverageDaysToPay { get; set; }
        public int OpenQuotesCount { get; set; }
        public int ActivePartRequestsCount { get; set; }
        public int LoyaltyPoints { get; set; }
        public string LoyaltyTier { get; set; } = string.Empty;
    }
}
