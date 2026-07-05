namespace SpareParts.Domain.Dashboard
{
    /// <summary>Unpaid-transactions insight panel: count, total, and the oldest items.</summary>
    public sealed class DashboardUnpaidTransactionsPanelDto
    {
        public int Count { get; set; }
        public decimal TotalRemaining { get; set; }
        public List<DashboardUnpaidTransactionItemDto> TopItems { get; set; } = new();
    }
}
