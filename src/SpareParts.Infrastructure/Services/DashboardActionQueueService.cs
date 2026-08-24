using Dapper;
using SpareParts.Domain.Dashboard;
using SpareParts.Domain.OwnerCockpit;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Infrastructure.Services
{
    /// <summary>
    /// Builds the server-side Action Queue for GET /api/dashboard/summary
    /// (Ignition Reports 04 §02 and 05 §4). It does not re-run the cockpit aggregation:
    /// items are derived from the OwnerCockpitDashboardDto the endpoint already loads,
    /// plus two cheap independent lookups (message failures, low stock) that run on their
    /// own DbSession so they can execute in parallel with the cockpit load.
    /// Items are ordered by severity × money at stake (Report 05 §4 scoring).
    /// </summary>
    public sealed class DashboardActionQueueService
    {
        private const int LowStockTopItems = 5;
        private const int MessageFailureLookbackHours = 48;

        private readonly ISqlConnectionFactory _factory;
        private readonly ITenantContext _tenantContext;

        public DashboardActionQueueService(ISqlConnectionFactory factory, ITenantContext tenantContext)
        {
            _factory = factory;
            _tenantContext = tenantContext;
        }

        /// <summary>Runs the independent signal lookups (Task B of the summary assembly).</summary>
        public DashboardActionQueueInputs GatherInputs(DateTime businessDate)
        {
            using var session = new DbSession(_factory, _tenantContext.TenantId);

            var failedMessages = session.Connection.ExecuteScalar<int>(
                @"
SELECT COUNT(1)
FROM dbo.OutboundMessages
WHERE Direction = N'Outbound'
  AND Status = N'Failed'
  AND CreatedAt >= @Since
  AND (@TenantId = 0 OR TenantId = @TenantId);",
                new { Since = DateTime.UtcNow.AddHours(-MessageFailureLookbackHours), session.TenantId },
                session.Transaction);

            var lowStockItems = session.Connection.Query<DashboardLowStockItemDto>(
                @"
SELECT TOP (@Top)
    parts.Id AS PartId,
    parts.Name,
    OnHand = ISNULL(stockSummary.OnHand, 0),
    MinStock = parts.MinStock,
    DeepLink = CONCAT(N'/reorder?partId=', CONVERT(NVARCHAR(20), parts.Id))
FROM dbo.Parts parts
OUTER APPLY
(
    SELECT SUM(CAST(stock.Quantity AS DECIMAL(19, 4))) AS OnHand
    FROM dbo.Stock stock
    WHERE stock.PartId = parts.Id
      AND (@TenantId = 0 OR stock.TenantId = @TenantId)
) stockSummary
WHERE parts.IsActive = 1
  AND parts.MinStock > 0
  AND ISNULL(stockSummary.OnHand, 0) <= parts.MinStock
  AND (@TenantId = 0 OR parts.TenantId = @TenantId)
ORDER BY parts.MinStock - ISNULL(stockSummary.OnHand, 0) DESC, parts.Id;",
                new { Top = LowStockTopItems, session.TenantId },
                session.Transaction).ToList();

            var lowStockCount = session.Connection.ExecuteScalar<int>(
                @"
SELECT COUNT(1)
FROM dbo.Parts parts
OUTER APPLY
(
    SELECT SUM(CAST(stock.Quantity AS DECIMAL(19, 4))) AS OnHand
    FROM dbo.Stock stock
    WHERE stock.PartId = parts.Id
      AND (@TenantId = 0 OR stock.TenantId = @TenantId)
) stockSummary
WHERE parts.IsActive = 1
  AND parts.MinStock > 0
  AND ISNULL(stockSummary.OnHand, 0) <= parts.MinStock
  AND (@TenantId = 0 OR parts.TenantId = @TenantId);",
                new { session.TenantId },
                session.Transaction);

            session.Commit();

            return new DashboardActionQueueInputs
            {
                FailedMessageCount = failedMessages,
                LowStockPanel = new DashboardLowStockPanelDto
                {
                    TotalCount = lowStockCount,
                    TopItems = lowStockItems
                }
            };
        }

        /// <summary>
        /// Assembles and scores the queue from the cockpit aggregate plus the gathered
        /// inputs. Pure in-memory — no database access.
        /// </summary>
        public List<DashboardActionQueueItemDto> Build(OwnerCockpitDashboardDto cockpit, DashboardActionQueueInputs inputs)
        {
            var items = new List<DashboardActionQueueItemDto>();
            var currencyCode = cockpit.CurrencyCode;

            if (cockpit.DailyProfitLoss.NetProfitLoss < 0m)
            {
                items.Add(new DashboardActionQueueItemDto
                {
                    Id = "negative-pnl",
                    Type = "profit",
                    Severity = "danger",
                    Title = "Review today's negative P&L",
                    Detail = $"Operating expenses exceed gross profit by {Math.Abs(cockpit.DailyProfitLoss.NetProfitLoss):N2} today.",
                    Amount = Math.Abs(cockpit.DailyProfitLoss.NetProfitLoss),
                    CurrencyCode = currencyCode,
                    DeepLink = "/accounting?view=daily-pnl"
                });
            }

            if (cockpit.UnpaidTransactionCount > 0)
            {
                items.Add(new DashboardActionQueueItemDto
                {
                    Id = "receivables-overdue",
                    Type = "receivables",
                    Severity = "danger",
                    Title = "Follow up unpaid transactions",
                    Detail = $"{cockpit.UnpaidTransactionCount:N0} open transactions waiting for payment.",
                    Amount = cockpit.UnpaidTransactionAmount,
                    CurrencyCode = currencyCode,
                    DeepLink = "/accounting?filter=unpaid"
                });
            }

            var redSegments = cockpit.ProfitHeatmap
                .Where(row => string.Equals(row.OverallSignal, "Red", StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (redSegments.Count > 0)
            {
                var names = redSegments
                    .Take(3)
                    .Select(row => row.CategoryName)
                    .Where(name => !string.IsNullOrWhiteSpace(name))
                    .ToList();

                items.Add(new DashboardActionQueueItemDto
                {
                    Id = "inventory-risk",
                    Type = "inventory",
                    Severity = "danger",
                    Title = "Triage red stock segments",
                    Detail = names.Count > 0
                        ? $"{redSegments.Count:N0} segments need margin, turnover, or dead-stock review: {string.Join(", ", names)}."
                        : $"{redSegments.Count:N0} segments need margin, turnover, or dead-stock review.",
                    Amount = null,
                    CurrencyCode = null,
                    DeepLink = "/stock?signal=red"
                });
            }

            var currencyAlert = cockpit.AccountingAlerts
                .FirstOrDefault(alert => alert.Title.Contains("Currency movement", StringComparison.OrdinalIgnoreCase));
            if (currencyAlert is not null)
            {
                items.Add(new DashboardActionQueueItemDto
                {
                    Id = "currency-margin",
                    Type = "pricing",
                    Severity = "warning",
                    Title = "Reprice currency-squeezed parts",
                    Detail = currencyAlert.Message,
                    Amount = null,
                    CurrencyCode = null,
                    DeepLink = "/pricing?signal=currency-squeeze"
                });
            }

            if (inputs.FailedMessageCount > 0)
            {
                items.Add(new DashboardActionQueueItemDto
                {
                    Id = "message-failures",
                    Type = "messages",
                    Severity = "warning",
                    Title = "Retry failed customer messages",
                    Detail = $"{inputs.FailedMessageCount:N0} recent WhatsApp messages failed delivery.",
                    Amount = null,
                    CurrencyCode = null,
                    DeepLink = "/whatsapp?status=failed"
                });
            }

            if (inputs.LowStockPanel.TotalCount > 0)
            {
                items.Add(new DashboardActionQueueItemDto
                {
                    Id = "low-stock",
                    Type = "inventory",
                    Severity = "warning",
                    Title = "Reorder low-stock parts",
                    Detail = $"{inputs.LowStockPanel.TotalCount:N0} active parts are at or below minimum stock.",
                    Amount = null,
                    CurrencyCode = null,
                    DeepLink = "/reorder"
                });
            }

            if (cockpit.TodaySalesCount == 0)
            {
                items.Add(new DashboardActionQueueItemDto
                {
                    Id = "no-sales-today",
                    Type = "sales",
                    Severity = "info",
                    Title = "No sales posted yet today",
                    Detail = "Start the first counter sale of the day.",
                    Amount = null,
                    CurrencyCode = null,
                    DeepLink = "/pos"
                });
            }

            return items
                .OrderByDescending(Score)
                .ThenBy(item => item.Id, StringComparer.Ordinal)
                .ToList();
        }

        /// <summary>severity × (1 + log10 of money at stake) — Report 05 §4.</summary>
        private static double Score(DashboardActionQueueItemDto item)
        {
            var severityWeight = item.Severity switch
            {
                "danger" => 100d,
                "warning" => 60d,
                _ => 10d
            };

            var money = (double)(item.Amount ?? 0m);
            return severityWeight * (1d + Math.Log10(1d + Math.Max(0d, money)));
        }
    }
}
