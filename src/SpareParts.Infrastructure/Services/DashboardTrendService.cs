using Dapper;
using SpareParts.Domain.Dashboard;
using SpareParts.Domain.Transactions;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Infrastructure.Services
{
    /// <summary>
    /// Loads the dashboard's daily sales trend series (Ignition Report 04 §02/§07).
    /// Runs on its own short-lived tenant-scoped DbSession so it can execute in parallel
    /// with the owner-cockpit aggregate — it is a point-in-time read-only snapshot and does
    /// not need transactional consistency with the core P&amp;L numbers.
    /// </summary>
    public sealed class DashboardTrendService
    {
        private readonly ISqlConnectionFactory _factory;
        private readonly ITenantContext _tenantContext;

        public DashboardTrendService(ISqlConnectionFactory factory, ITenantContext tenantContext)
        {
            _factory = factory;
            _tenantContext = tenantContext;
        }

        public DashboardSalesTrendDto GetSalesTrend(DateTime businessDate, int rangeDays = 7)
        {
            var date = businessDate.Date;
            rangeDays = Math.Clamp(rangeDays, 1, 90);
            var rangeStart = date.AddDays(-(rangeDays - 1));
            var rangeEndExclusive = date.AddDays(1);

            using var session = new DbSession(_factory, _tenantContext.TenantId);
            var currencyContext = AccountingCurrencyContextResolver.Resolve(session);

            // Same Transactions + TransactionTypes join and display-amount shape as the
            // owner-cockpit summary, grouped by transaction date instead of summed to one row.
            const string sql = @"
SELECT
    CAST(t.TransactionDate AS DATE) AS SaleDate,
    Amount = ISNULL(SUM(CASE WHEN t.IsReturn = 1 THEN -amounts.DisplayTotalAmount ELSE amounts.DisplayTotalAmount END), 0)
FROM dbo.Transactions t
INNER JOIN dbo.TransactionTypes tt ON tt.Id = t.TransactionTypeId AND tt.TypeKey = @SaleTypeKey
CROSS APPLY
(
    SELECT DisplayTotalAmount = COALESCE(
        NULLIF(t.TotalCounterAmount, 0),
        CASE WHEN @CounterRateToBase > 0 THEN COALESCE(NULLIF(t.TotalBaseAmount, 0), t.TotalAmount, 0) / @CounterRateToBase ELSE t.TotalAmount END)
) amounts
WHERE t.TransactionDate >= @RangeStart
  AND t.TransactionDate < @RangeEndExclusive
  AND (@TenantId = 0 OR t.TenantId = @TenantId)
GROUP BY CAST(t.TransactionDate AS DATE)
ORDER BY SaleDate;";

            var rows = session.Connection.Query<DashboardTrendRow>(
                sql,
                new
                {
                    RangeStart = rangeStart,
                    RangeEndExclusive = rangeEndExclusive,
                    SaleTypeKey = TransactionTypeKeys.Sale,
                    CounterRateToBase = currencyContext.CounterRateToBase > 0m ? currencyContext.CounterRateToBase : 1m,
                    session.TenantId
                },
                session.Transaction).ToDictionary(row => row.SaleDate.Date, row => row.Amount);

            session.Commit();

            var series = new List<DashboardTrendPointDto>(rangeDays);
            for (var day = rangeStart; day <= date; day = day.AddDays(1))
            {
                series.Add(new DashboardTrendPointDto
                {
                    Date = day,
                    Amount = rows.TryGetValue(day, out var amount) ? decimal.Round(amount, 4, MidpointRounding.AwayFromZero) : 0m
                });
            }

            var peak = series.Count > 0 ? series.MaxBy(point => point.Amount) : null;
            return new DashboardSalesTrendDto
            {
                Granularity = "daily",
                RangeDays = rangeDays,
                Series = series,
                PeakDate = peak?.Date,
                PeakAmount = peak?.Amount
            };
        }
    }
}
