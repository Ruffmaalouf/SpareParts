namespace SpareParts.Domain.Dashboard
{
    /// <summary>
    /// Composed read model for GET /api/dashboard/summary (Ignition Report 04 §02–03).
    /// Wraps the existing owner-cockpit aggregate with the server-built action queue,
    /// sales trend series, and the three insight panels the dashboard binds to.
    /// Deliberately carries no tenant id, database name, or cache metadata (Report 04 §09).
    /// </summary>
    public sealed class DashboardSummaryDto
    {
        public DateTime BusinessDate { get; set; }
        public DateTime GeneratedAt { get; set; }
        public string CurrencyCode { get; set; } = "USD";
        public DashboardKpisDto Kpis { get; set; } = new();
        public List<DashboardActionQueueItemDto> ActionQueue { get; set; } = new();
        public DashboardSalesTrendDto SalesTrend { get; set; } = new();
        public DashboardLowStockPanelDto LowStockPanel { get; set; } = new();
        public DashboardDeadStockPanelDto DeadStockPanel { get; set; } = new();
        public DashboardUnpaidTransactionsPanelDto UnpaidTransactionsPanel { get; set; } = new();
    }
}
