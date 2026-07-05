using SpareParts.Domain.Dashboard;
using SpareParts.Domain.OwnerCockpit;

namespace SpareParts.Infrastructure.Services
{
    /// <summary>
    /// Composes GET /api/dashboard/summary (Ignition Report 04 §02): the existing
    /// OwnerCockpitService aggregate (unchanged, one DbSession for internal consistency)
    /// plus the action-queue inputs and trend series, which are read-only point-in-time
    /// snapshots and therefore run in parallel on their own short-lived connections
    /// (Task.WhenAll over three tasks — Fig. 1 of the report).
    /// Caching and ETag handling live at the API layer, keeping this service reusable
    /// and cache-agnostic like OwnerCockpitService itself.
    /// </summary>
    public sealed class DashboardService
    {
        private const int TrendRangeDays = 7;
        private const int DeadStockTopSegments = 5;
        private const int UnpaidTopItems = 5;

        private readonly OwnerCockpitService _ownerCockpitService;
        private readonly DashboardActionQueueService _actionQueueService;
        private readonly DashboardTrendService _trendService;

        public DashboardService(
            OwnerCockpitService ownerCockpitService,
            DashboardActionQueueService actionQueueService,
            DashboardTrendService trendService)
        {
            _ownerCockpitService = ownerCockpitService;
            _actionQueueService = actionQueueService;
            _trendService = trendService;
        }

        public DashboardSummaryDto GetSummary(DateTime? businessDate)
        {
            var date = (businessDate ?? DateTime.Today).Date;

            // Task A: cockpit aggregate (session 1) · Task B: queue inputs (session 2)
            // Task C: trend series (session 3). Independent connections, so true parallelism.
            var cockpitTask = Task.Run(() => _ownerCockpitService.GetDashboard(date));
            var inputsTask = Task.Run(() => _actionQueueService.GatherInputs(date));
            var trendTask = Task.Run(() => _trendService.GetSalesTrend(date, TrendRangeDays));

            Task.WaitAll(cockpitTask, inputsTask, trendTask);

            var cockpit = cockpitTask.Result;
            var inputs = inputsTask.Result;
            var trend = trendTask.Result;

            return new DashboardSummaryDto
            {
                BusinessDate = cockpit.BusinessDate,
                GeneratedAt = DateTime.UtcNow,
                CurrencyCode = cockpit.CurrencyCode,
                Kpis = BuildKpis(cockpit, inputs),
                ActionQueue = _actionQueueService.Build(cockpit, inputs),
                SalesTrend = trend,
                LowStockPanel = inputs.LowStockPanel,
                DeadStockPanel = BuildDeadStockPanel(cockpit),
                UnpaidTransactionsPanel = BuildUnpaidPanel(cockpit)
            };
        }

        private static DashboardKpisDto BuildKpis(OwnerCockpitDashboardDto cockpit, DashboardActionQueueInputs inputs)
            => new()
            {
                SalesToday = Kpi(cockpit.TodaySalesAmount, cockpit.SalesChangePercent),
                ProfitToday = Kpi(cockpit.TodaySalesProfit, cockpit.ProfitChangePercent),
                CashBalance = Kpi(cockpit.CashBalance, null),
                Receivables = Kpi(cockpit.CustomerDebt, null),
                StockValue = Kpi(cockpit.StockValue, null),
                LowStockCount = Kpi(inputs.LowStockPanel.TotalCount, null)
            };

        private static DashboardKpiDto Kpi(decimal value, decimal? deltaPercent)
            => new()
            {
                Value = value,
                DeltaPercent = deltaPercent,
                Direction = deltaPercent switch
                {
                    > 0m => "up",
                    < 0m => "down",
                    _ => "flat"
                }
            };

        private static DashboardDeadStockPanelDto BuildDeadStockPanel(OwnerCockpitDashboardDto cockpit)
        {
            var segments = cockpit.ProfitHeatmap
                .Where(row => row.DeadStockUnits > 0m || row.DeadStockValue > 0m)
                .OrderByDescending(row => row.DeadStockValue)
                .ToList();

            return new DashboardDeadStockPanelDto
            {
                TotalValue = segments.Sum(row => row.DeadStockValue),
                TotalUnits = segments.Sum(row => row.DeadStockUnits),
                TopSegments = segments
                    .Take(DeadStockTopSegments)
                    .Select(row => new DashboardDeadStockSegmentDto
                    {
                        SegmentKey = row.SegmentKey,
                        CategoryName = row.CategoryName,
                        DeadStockValue = row.DeadStockValue,
                        DeadStockUnits = row.DeadStockUnits
                    })
                    .ToList()
            };
        }

        private static DashboardUnpaidTransactionsPanelDto BuildUnpaidPanel(OwnerCockpitDashboardDto cockpit)
        {
            var today = cockpit.BusinessDate.Date;
            return new DashboardUnpaidTransactionsPanelDto
            {
                Count = cockpit.UnpaidTransactionCount,
                TotalRemaining = cockpit.UnpaidTransactionAmount,
                TopItems = cockpit.UnpaidTransactions
                    .OrderByDescending(row => row.RemainingAmount)
                    .Take(UnpaidTopItems)
                    .Select(row => new DashboardUnpaidTransactionItemDto
                    {
                        TransactionNumber = row.TransactionNumber,
                        Counterparty = row.Counterparty,
                        RemainingAmount = row.RemainingAmount,
                        DaysOverdue = Math.Max(0, (int)(today - row.TransactionDate.Date).TotalDays)
                    })
                    .ToList()
            };
        }
    }
}
