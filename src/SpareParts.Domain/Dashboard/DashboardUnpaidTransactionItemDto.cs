namespace SpareParts.Domain.Dashboard
{
    /// <summary>One unpaid transaction surfaced on the dashboard panel.</summary>
    public sealed class DashboardUnpaidTransactionItemDto
    {
        public string TransactionNumber { get; set; } = string.Empty;
        public string Counterparty { get; set; } = string.Empty;
        public decimal RemainingAmount { get; set; }
        public int DaysOverdue { get; set; }
    }
}
