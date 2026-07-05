using System;
using System.Collections.Generic;

namespace SpareParts.Desktop.Wpf.Interfaces
{
    /// <summary>
    /// Composed read model returned by GET /api/dashboard/summary (Ignition redesign, Report 04 §02–§03).
    /// Field names mirror the documented camelCase JSON contract 1:1 (deserialization is case-insensitive).
    /// </summary>
    public sealed class IgnitionDashboardSummaryDto
    {
        public DateTime BusinessDate { get; set; }
        public DateTime GeneratedAt { get; set; }
        public string CurrencyCode { get; set; } = "USD";
        public IgnitionDashboardKpisDto Kpis { get; set; } = new();
        public List<IgnitionActionQueueItemDto> ActionQueue { get; set; } = new();
        public IgnitionSalesTrendDto SalesTrend { get; set; } = new();
        public IgnitionLowStockPanelDto LowStockPanel { get; set; } = new();
        public IgnitionDeadStockPanelDto DeadStockPanel { get; set; } = new();
        public IgnitionUnpaidTransactionsPanelDto UnpaidTransactionsPanel { get; set; } = new();
        public IgnitionCacheMetaDto CacheMeta { get; set; } = new();
    }
}
