namespace SpareParts.Desktop.Wpf.Interfaces
{
    /// <summary>Unpaid-transactions panel row of GET /api/dashboard/summary (Report 04 §03).</summary>
    public sealed class IgnitionUnpaidTransactionItemDto
    {
        public string TransactionNumber { get; set; } = string.Empty;
        public string Counterparty { get; set; } = string.Empty;
        public decimal RemainingAmount { get; set; }
        public int DaysOverdue { get; set; }
    }
}
