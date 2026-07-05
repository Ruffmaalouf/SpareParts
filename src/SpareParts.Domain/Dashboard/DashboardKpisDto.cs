namespace SpareParts.Domain.Dashboard
{
    /// <summary>
    /// KPI band for the Ignition dashboard. Mirrors — not replaces — the flat fields on
    /// OwnerCockpitDashboardDto; values are mapped from that aggregate, never re-queried.
    /// </summary>
    public sealed class DashboardKpisDto
    {
        public DashboardKpiDto SalesToday { get; set; } = new();
        public DashboardKpiDto ProfitToday { get; set; } = new();
        public DashboardKpiDto CashBalance { get; set; } = new();
        public DashboardKpiDto Receivables { get; set; } = new();
        public DashboardKpiDto StockValue { get; set; } = new();
        public DashboardKpiDto LowStockCount { get; set; } = new();
    }
}
