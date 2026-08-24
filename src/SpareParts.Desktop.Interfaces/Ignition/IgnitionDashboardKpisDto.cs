namespace SpareParts.Desktop.Wpf.Interfaces
{
    /// <summary>The six-KPI band of GET /api/dashboard/summary (Report 04 §03).</summary>
    public sealed class IgnitionDashboardKpisDto
    {
        public IgnitionKpiValueDto SalesToday { get; set; } = new();
        public IgnitionKpiValueDto ProfitToday { get; set; } = new();
        public IgnitionKpiValueDto CashBalance { get; set; } = new();
        public IgnitionKpiValueDto Receivables { get; set; } = new();
        public IgnitionKpiValueDto StockValue { get; set; } = new();
        public IgnitionKpiValueDto LowStockCount { get; set; } = new();
    }
}
