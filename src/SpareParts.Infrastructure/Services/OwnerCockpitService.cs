using Dapper;
using SpareParts.Domain.Accounting;
using SpareParts.Domain.OwnerCockpit;
using SpareParts.Domain.Transactions;
using SpareParts.Infrastructure.Data;
using SpareParts.Infrastructure.Interfaces;

namespace SpareParts.Infrastructure.Services
{
    public sealed class OwnerCockpitService
    {
        private const string SaleReferenceType = "Sale";
        private readonly ISqlConnectionFactory _factory;

        public OwnerCockpitService(ISqlConnectionFactory factory)
        {
            _factory = factory;
        }

        public OwnerCockpitDashboardDto GetDashboard(DateTime? businessDate)
        {
            var date = (businessDate ?? DateTime.Today).Date;
            var nextDate = date.AddDays(1);

            using var session = new DbSession(_factory);
            var currencyContext = AccountingCurrencyContextResolver.Resolve(session);
            var summary = LoadSummary(session, date, nextDate, currencyContext);
            var dailyProfitLoss = LoadDailyProfitLoss(session, date, nextDate, summary, currencyContext);
            var cashBalance = ConvertBaseToCounter(LoadCashBalance(session), currencyContext);
            var profitPerPart = LoadProfitPerPart(session, date, nextDate, currencyContext);
            var profitPerCar = LoadProfitPerCar(session, currencyContext);
            var profitHeatmap = LoadProfitHeatmap(session, date, nextDate, currencyContext);
            var unpaidTransactions = LoadUnpaidTransactions(session, currencyContext);
            var alerts = LoadAlerts(session, date, nextDate, currencyContext);

            session.Commit();

            return new OwnerCockpitDashboardDto
            {
                BusinessDate = date,
                GeneratedAt = DateTime.Now,
                CurrencyCode = currencyContext.CounterCurrencyCode,
                TodaySalesCount = summary.TodaySalesCount,
                TodaySalesAmount = summary.TodaySalesAmount,
                TodaySalesPaidAmount = summary.TodaySalesPaidAmount,
                TodaySalesProfit = summary.TodaySalesProfit,
                TodayPurchasesCount = summary.TodayPurchasesCount,
                TodayPurchasesAmount = summary.TodayPurchasesAmount,
                TodayPurchasesPaidAmount = summary.TodayPurchasesPaidAmount,
                CashBalance = cashBalance,
                SupplierDebt = summary.SupplierDebt,
                CustomerDebt = summary.CustomerDebt,
                StockValue = summary.StockValue,
                TotalCarProfit = profitPerCar.Sum(row => row.Profit),
                TotalPartProfit = profitPerPart.Sum(row => row.Profit),
                UnpaidTransactionCount = unpaidTransactions.Count,
                UnpaidTransactionAmount = unpaidTransactions.Sum(row => row.RemainingAmount),
                AccountingAlertCount = alerts.Count,
                DailyProfitLoss = dailyProfitLoss,
                ProfitPerCar = profitPerCar,
                ProfitPerPart = profitPerPart,
                ProfitHeatmap = profitHeatmap,
                UnpaidTransactions = unpaidTransactions,
                AccountingAlerts = alerts
            };
        }

        private static OwnerCockpitSummaryRow LoadSummary(
            DbSession session,
            DateTime businessDate,
            DateTime nextBusinessDate,
            AccountingCurrencyContext currencyContext)
        {
            const string sql = @"
WITH TransactionAmounts AS
(
    SELECT
        t.*,
        DisplayTotalAmount = COALESCE(NULLIF(t.TotalCounterAmount, 0), CASE WHEN @CounterRateToBase > 0 THEN COALESCE(NULLIF(t.TotalBaseAmount, 0), t.TotalAmount, 0) / @CounterRateToBase ELSE t.TotalAmount END),
        DisplayPaidAmount = COALESCE(NULLIF(t.PaidCounterAmount, 0), CASE WHEN @CounterRateToBase > 0 THEN COALESCE(NULLIF(t.PaidAmount, 0), 0) / @CounterRateToBase ELSE t.PaidAmount END),
        DisplayTotalCost = CASE WHEN @CounterRateToBase > 0 THEN ISNULL(t.TotalCost, 0) / @CounterRateToBase ELSE ISNULL(t.TotalCost, 0) END
    FROM dbo.Transactions t
),
TransactionSummary AS
(
    SELECT
    TodaySalesCount = ISNULL(SUM(CASE
        WHEN tt.TypeKey = @SaleTypeKey
         AND t.TransactionDate >= @BusinessDate
         AND t.TransactionDate < @NextBusinessDate
        THEN 1 ELSE 0 END), 0),
    TodaySalesAmount = ISNULL(SUM(CASE
        WHEN tt.TypeKey = @SaleTypeKey
         AND t.TransactionDate >= @BusinessDate
         AND t.TransactionDate < @NextBusinessDate
        THEN CASE WHEN t.IsReturn = 1 THEN -t.DisplayTotalAmount ELSE t.DisplayTotalAmount END
        ELSE 0 END), 0),
    TodaySalesPaidAmount = ISNULL(SUM(CASE
        WHEN tt.TypeKey = @SaleTypeKey
         AND t.TransactionDate >= @BusinessDate
         AND t.TransactionDate < @NextBusinessDate
        THEN CASE WHEN t.IsReturn = 1 THEN -t.DisplayPaidAmount ELSE t.DisplayPaidAmount END
        ELSE 0 END), 0),
    TodaySalesProfit = ISNULL(SUM(CASE
        WHEN tt.TypeKey = @SaleTypeKey
         AND t.TransactionDate >= @BusinessDate
         AND t.TransactionDate < @NextBusinessDate
        THEN CASE WHEN t.IsReturn = 1 THEN -(t.DisplayTotalAmount - t.DisplayTotalCost) ELSE t.DisplayTotalAmount - t.DisplayTotalCost END
        ELSE 0 END), 0),
    TodayPurchasesCount = ISNULL(SUM(CASE
        WHEN tt.TypeKey IN (@PurchaseTypeKey, @UsedCarPurchaseTypeKey)
         AND t.TransactionDate >= @BusinessDate
         AND t.TransactionDate < @NextBusinessDate
        THEN 1 ELSE 0 END), 0),
    TodayPurchasesAmount = ISNULL(SUM(CASE
        WHEN tt.TypeKey IN (@PurchaseTypeKey, @UsedCarPurchaseTypeKey)
         AND t.TransactionDate >= @BusinessDate
         AND t.TransactionDate < @NextBusinessDate
        THEN t.DisplayTotalAmount ELSE 0 END), 0),
    TodayPurchasesPaidAmount = ISNULL(SUM(CASE
        WHEN tt.TypeKey IN (@PurchaseTypeKey, @UsedCarPurchaseTypeKey)
         AND t.TransactionDate >= @BusinessDate
         AND t.TransactionDate < @NextBusinessDate
        THEN t.DisplayPaidAmount ELSE 0 END), 0),
    CustomerDebt = ISNULL(SUM(CASE
        WHEN tt.TypeKey = @SaleTypeKey
         AND t.DisplayTotalAmount > t.DisplayPaidAmount
        THEN t.DisplayTotalAmount - t.DisplayPaidAmount ELSE 0 END), 0),
    SupplierDebt = ISNULL(SUM(CASE
        WHEN tt.TypeKey IN (@PurchaseTypeKey, @UsedCarPurchaseTypeKey)
         AND t.DisplayTotalAmount > t.DisplayPaidAmount
        THEN t.DisplayTotalAmount - t.DisplayPaidAmount ELSE 0 END), 0),
    StockValue = (
        SELECT ISNULL(SUM(CAST(s.Quantity AS DECIMAL(19, 4)) * COALESCE(NULLIF(p.AllocatedCost, 0), NULLIF(p.CostPrice, 0), NULLIF(p.AveragePrice, 0), 0)), 0) / CASE WHEN @CounterRateToBase > 0 THEN @CounterRateToBase ELSE 1 END
        FROM dbo.Stock s
        INNER JOIN dbo.Parts p ON p.Id = s.PartId
    )
    FROM TransactionAmounts t
    INNER JOIN dbo.TransactionTypes tt ON tt.Id = t.TransactionTypeId
),
WholesaleAmounts AS
(
    SELECT
        w.Id,
        DisplaySaleAmount = COALESCE(NULLIF(w.SalePriceCounter, 0), CASE WHEN rate.EffectiveCounterRate > 0 THEN ISNULL(w.SalePriceBase, 0) / rate.EffectiveCounterRate ELSE ISNULL(w.SalePrice, 0) END),
        DisplayPaidAmount = COALESCE(NULLIF(w.PaidCounterAmount, 0), CASE WHEN rate.EffectiveCounterRate > 0 THEN ISNULL(w.PaidBaseAmount, 0) / rate.EffectiveCounterRate ELSE ISNULL(w.PaidAmount, 0) END),
        DisplayCost = CASE
            WHEN rate.EffectiveCounterRate > 0 THEN (cost.FullCostBase + ISNULL(w.RepairTotalBaseAmount, 0)) / rate.EffectiveCounterRate
            ELSE cost.FullCostBase + ISNULL(w.RepairTotalBaseAmount, 0)
        END
    FROM dbo.UsedCarWholesaleSales w
    INNER JOIN dbo.UsedCars uc ON uc.Id = w.UsedCarId
    CROSS APPLY
    (
        SELECT EffectiveCounterRate = COALESCE(NULLIF(w.CounterRateToBase, 0), NULLIF(uc.CounterRateToBase, 0), NULLIF(@CounterRateToBase, 0), 1)
    ) rate
    CROSS APPLY
    (
        SELECT FullCostBase = CASE
            WHEN ISNULL(uc.GrandTotalBase, 0) > 0 THEN ISNULL(uc.GrandTotalBase, 0)
            ELSE ISNULL(uc.PriceBase, 0)
                + ((ISNULL(uc.Transportation, 0) + ISNULL(uc.PartOutAmount, 0) + ISNULL(uc.Shipping, 0) + ISNULL(uc.Customs, 0) + ISNULL(uc.Repairs, 0)) * rate.EffectiveCounterRate)
        END
    ) cost
    WHERE w.SaleDate >= @BusinessDate
      AND w.SaleDate < @NextBusinessDate
),
WholesaleSummary AS
(
    SELECT
        TodaySalesCount = COUNT(1),
        TodaySalesAmount = ISNULL(SUM(DisplaySaleAmount), 0),
        TodaySalesPaidAmount = ISNULL(SUM(DisplayPaidAmount), 0),
        TodaySalesProfit = ISNULL(SUM(DisplaySaleAmount - DisplayCost), 0),
        CustomerDebt = ISNULL(SUM(CASE WHEN DisplaySaleAmount > DisplayPaidAmount THEN DisplaySaleAmount - DisplayPaidAmount ELSE 0 END), 0)
    FROM WholesaleAmounts
)
SELECT
    TodaySalesCount = transactions.TodaySalesCount + wholesale.TodaySalesCount,
    TodaySalesAmount = transactions.TodaySalesAmount + wholesale.TodaySalesAmount,
    TodaySalesPaidAmount = transactions.TodaySalesPaidAmount + wholesale.TodaySalesPaidAmount,
    TodaySalesProfit = transactions.TodaySalesProfit + wholesale.TodaySalesProfit,
    transactions.TodayPurchasesCount,
    transactions.TodayPurchasesAmount,
    transactions.TodayPurchasesPaidAmount,
    CustomerDebt = transactions.CustomerDebt + wholesale.CustomerDebt,
    transactions.SupplierDebt,
    transactions.StockValue
FROM TransactionSummary transactions
CROSS JOIN WholesaleSummary wholesale;";

            return session.Connection.QuerySingle<OwnerCockpitSummaryRow>(
                sql,
                new
                {
                    BusinessDate = businessDate,
                    NextBusinessDate = nextBusinessDate,
                    SaleTypeKey = TransactionTypeKeys.Sale,
                    PurchaseTypeKey = TransactionTypeKeys.Purchase,
                    UsedCarPurchaseTypeKey = TransactionTypeKeys.UsedCarPurchase,
                    CounterRateToBase = ResolveCounterRateToBase(currencyContext)
                },
                session.Transaction);
        }

        private static OwnerCockpitDailyProfitLossDto LoadDailyProfitLoss(
            DbSession session,
            DateTime businessDate,
            DateTime nextBusinessDate,
            OwnerCockpitSummaryRow summary,
            AccountingCurrencyContext currencyContext)
        {
            var expenseBreakdown = LoadDailyOperatingExpenses(session, businessDate, nextBusinessDate, currencyContext);
            return OwnerCockpitDailyProfitLossCalculator.Build(
                businessDate,
                currencyContext.CounterCurrencyCode,
                summary.TodaySalesAmount,
                summary.TodaySalesProfit,
                expenseBreakdown);
        }

        private static List<OwnerCockpitExpenseBreakdownRowDto> LoadDailyOperatingExpenses(
            DbSession session,
            DateTime businessDate,
            DateTime nextBusinessDate,
            AccountingCurrencyContext currencyContext)
        {
            var hasAccountTypeKey = AccountingSchemaInspector.HasColumn(session, "dbo.Accounts", "AccountTypeKey");
            var accountTypeSource = hasAccountTypeKey ? "a.AccountTypeKey" : "a.AccountType";
            var normalizedAccountTypeKey = AccountingSql.NormalizeAccountTypeKey(accountTypeSource);
            var counterRateToBase = ResolveCounterRateToBase(currencyContext);
            var sql = $@"
SELECT
    je.Id AS JournalEntryId,
    a.Code AS AccountCode,
    a.Name AS AccountName,
    je.Description,
    Amount = CASE
        WHEN jl.Debit > 0 THEN
            COALESCE(NULLIF(jl.CounterAmount, 0), CASE WHEN @CounterRateToBase > 0 THEN jl.Debit / @CounterRateToBase ELSE jl.Debit END)
        ELSE
            -COALESCE(NULLIF(jl.CounterAmount, 0), CASE WHEN @CounterRateToBase > 0 THEN jl.Credit / @CounterRateToBase ELSE jl.Credit END)
    END
FROM dbo.JournalLines jl
INNER JOIN dbo.JournalEntries je ON je.Id = jl.JournalEntryId
INNER JOIN dbo.Accounts a ON a.Id = jl.AccountId
LEFT JOIN dbo.AccountingPostingSettings cogsSetting ON cogsSetting.SettingKey = @CogsSettingKey
WHERE je.EntryDate >= @BusinessDate
  AND je.EntryDate < @NextBusinessDate
  AND {normalizedAccountTypeKey} = @ExpenseAccountTypeKey
  AND (cogsSetting.AccountId IS NULL OR a.Id <> cogsSetting.AccountId)
  AND NOT EXISTS
  (
      SELECT 1
      FROM dbo.AccountingPostingSettings usedCarCostSetting
      WHERE usedCarCostSetting.AccountId = a.Id
        AND usedCarCostSetting.SettingKey IN @UsedCarCostSettingKeys
  );";

            var rows = session.Connection.Query<OwnerCockpitExpenseJournalLineRow>(
                sql,
                new
                {
                    BusinessDate = businessDate,
                    NextBusinessDate = nextBusinessDate,
                    CounterRateToBase = counterRateToBase,
                    CogsSettingKey = AccountingSettingKeys.Cogs,
                    UsedCarCostSettingKeys = new[]
                    {
                        AccountingSettingKeys.UsedCarTransportation,
                        AccountingSettingKeys.UsedCarPartOut,
                        AccountingSettingKeys.UsedCarShipping,
                        AccountingSettingKeys.UsedCarCustoms,
                        AccountingSettingKeys.UsedCarRepairs
                    },
                    ExpenseAccountTypeKey = "expense"
                },
                session.Transaction);

            return rows
                .Select(row => new
                {
                    Category = OwnerCockpitDailyProfitLossCalculator.ClassifyExpense(row.AccountCode, row.AccountName, row.Description),
                    row.AccountCode,
                    row.AccountName,
                    row.JournalEntryId,
                    row.Amount
                })
                .GroupBy(row => new { row.Category, row.AccountCode, row.AccountName })
                .Select(group => new OwnerCockpitExpenseBreakdownRowDto
                {
                    Category = group.Key.Category,
                    AccountCode = group.Key.AccountCode ?? string.Empty,
                    AccountName = group.Key.AccountName ?? string.Empty,
                    Amount = decimal.Round(group.Sum(row => row.Amount), 4, MidpointRounding.AwayFromZero),
                    EntryCount = group.Select(row => row.JournalEntryId).Distinct().Count()
                })
                .OrderBy(row => ExpenseCategoryRank(row.Category))
                .ThenBy(row => row.AccountCode)
                .ThenBy(row => row.AccountName)
                .ToList();
        }

        private static decimal LoadCashBalance(DbSession session)
        {
            const string sql = @"
DECLARE @CashAccountId INT;

SELECT TOP (1) @CashAccountId = AccountId
FROM dbo.AccountingPostingSettings
WHERE SettingKey = @SalesCashKey;

IF @CashAccountId IS NULL
BEGIN
    SELECT TOP (1) @CashAccountId = Id
    FROM dbo.Accounts
    WHERE Code = N'1000';
END;

;WITH AccountTree AS
(
    SELECT Id
    FROM dbo.Accounts
    WHERE Id = @CashAccountId

    UNION ALL

    SELECT child.Id
    FROM dbo.Accounts child
    INNER JOIN AccountTree parent ON parent.Id = child.ParentId
)
SELECT ISNULL(SUM(jl.Debit - jl.Credit), 0)
FROM AccountTree tree
LEFT JOIN dbo.JournalLines jl ON jl.AccountId = tree.Id;";

            return session.Connection.ExecuteScalar<decimal>(
                sql,
                new { SalesCashKey = AccountingSettingKeys.SalesCash },
                session.Transaction);
        }

        private static List<OwnerCockpitProfitRowDto> LoadProfitPerPart(
            DbSession session,
            DateTime businessDate,
            DateTime nextBusinessDate,
            AccountingCurrencyContext currencyContext)
        {
            const string sql = @"
WITH PurchaseRates AS
(
    SELECT
        ti.PartId,
        PurchaseRateToBase =
            SUM(
                ABS(ISNULL(ti.Quantity, 0))
                * COALESCE(
                    CASE
                        WHEN ABS(ISNULL(ti.CounterAmount, 0)) > 0 AND ABS(ISNULL(ti.BaseAmount, 0)) > 0
                        THEN ABS(ti.BaseAmount) / ABS(ti.CounterAmount)
                        ELSE NULL
                    END,
                    NULLIF(ti.RateToBase, 0),
                    CASE
                        WHEN ABS(ISNULL(t.TotalCounterAmount, 0)) > 0 AND ABS(ISNULL(t.TotalBaseAmount, 0)) > 0
                        THEN ABS(t.TotalBaseAmount) / ABS(t.TotalCounterAmount)
                        ELSE NULL
                    END,
                    NULLIF(@CounterRateToBase, 0),
                    1
                )
            ) / NULLIF(SUM(ABS(ISNULL(ti.Quantity, 0))), 0)
    FROM dbo.Transactions t
    INNER JOIN dbo.TransactionTypes tt ON tt.Id = t.TransactionTypeId AND tt.TypeKey = @PurchaseTypeKey
    INNER JOIN dbo.TransactionItems ti ON ti.TransactionId = t.Id
    WHERE ti.PartId IS NOT NULL
      AND UPPER(LTRIM(RTRIM(ISNULL(NULLIF(t.CounterCurrencyCode, N''), @CounterCurrencyCode)))) = UPPER(LTRIM(RTRIM(@CounterCurrencyCode)))
    GROUP BY ti.PartId
),
LineRows AS
(
    SELECT
        p.Id AS PartId,
        Name = CONCAT(COALESCE(NULLIF(p.InternalCode, N''), CONVERT(NVARCHAR(20), p.Id)), N' - ', p.Name),
        QuantitySold = SUM(ABS(ISNULL(ti.Quantity, 0))),
        Revenue = SUM(CASE
            WHEN t.IsReturn = 1 THEN -COALESCE(NULLIF(ti.CounterAmount, 0), ISNULL(ti.LineTotal, 0))
            ELSE COALESCE(NULLIF(ti.CounterAmount, 0), ISNULL(ti.LineTotal, 0))
        END),
        CostBase = SUM(CASE
            WHEN t.IsReturn = 1 THEN -COALESCE(NULLIF(m.MovementCost, 0), ABS(ISNULL(ti.Quantity, 0)) * COALESCE(NULLIF(p.AllocatedCost, 0), NULLIF(p.CostPrice, 0), NULLIF(p.AveragePrice, 0), 0))
            ELSE COALESCE(NULLIF(m.MovementCost, 0), ABS(ISNULL(ti.Quantity, 0)) * COALESCE(NULLIF(p.AllocatedCost, 0), NULLIF(p.CostPrice, 0), NULLIF(p.AveragePrice, 0), 0))
        END),
        PurchaseRateToBase = COALESCE(
            NULLIF(MAX(CASE
                WHEN uc.Id IS NOT NULL
                 AND UPPER(LTRIM(RTRIM(ISNULL(NULLIF(uc.CounterCurrencyCode, N''), @CounterCurrencyCode)))) = UPPER(LTRIM(RTRIM(@CounterCurrencyCode)))
                THEN uc.CounterRateToBase
                ELSE NULL
            END), 0),
            NULLIF(MAX(pr.PurchaseRateToBase), 0),
            NULLIF(MAX(CASE WHEN ISNULL(ti.RateToBase, 0) > 0 THEN ti.RateToBase ELSE NULL END), 0),
            NULLIF(@CounterRateToBase, 0),
            1)
    FROM dbo.Transactions t
    INNER JOIN dbo.TransactionTypes tt ON tt.Id = t.TransactionTypeId AND tt.TypeKey = @SaleTypeKey
    INNER JOIN dbo.TransactionItems ti ON ti.TransactionId = t.Id
    INNER JOIN dbo.Parts p ON p.Id = ti.PartId
    LEFT JOIN dbo.UsedCars uc ON uc.Id = p.UsedCarId
    LEFT JOIN PurchaseRates pr ON pr.PartId = p.Id
    OUTER APPLY
    (
        SELECT SUM(ABS(CAST(sm.Quantity AS DECIMAL(19, 4))) * sm.UnitCost) AS MovementCost
        FROM dbo.StockMovements sm
        WHERE sm.ReferenceType = @SaleReferenceType
          AND sm.ReferenceId = t.ReferenceId
          AND sm.PartId = ti.PartId
    ) m
    WHERE t.TransactionDate >= @BusinessDate
      AND t.TransactionDate < @NextBusinessDate
      AND ti.PartId IS NOT NULL
    GROUP BY p.Id, p.InternalCode, p.Name
),
Margins AS
(
    SELECT
        Name,
        Subtitle = CONCAT(CONVERT(NVARCHAR(32), QuantitySold), N' sold today'),
        Revenue = ROUND(Revenue, 4),
        Cost = ROUND(CostAtCurrentRate, 4),
        Profit = ROUND(Revenue - CostAtCurrentRate, 4),
        MarginAtPurchaseRate = ROUND(Revenue - CostAtPurchaseRate, 4),
        MarginAtCurrentRate = ROUND(Revenue - CostAtCurrentRate, 4),
        CurrencyMovementImpact = ROUND((Revenue - CostAtCurrentRate) - (Revenue - CostAtPurchaseRate), 4),
        PurchaseRateToBase = EffectivePurchaseRate,
        CurrentRateToBase = EffectiveCurrentRate
    FROM LineRows
    CROSS APPLY
    (
        SELECT
            EffectivePurchaseRate = CASE WHEN PurchaseRateToBase > 0 THEN PurchaseRateToBase ELSE 1 END,
            EffectiveCurrentRate = CASE WHEN @CounterRateToBase > 0 THEN @CounterRateToBase ELSE 1 END
    ) rates
    CROSS APPLY
    (
        SELECT
            CostAtPurchaseRate = CASE WHEN rates.EffectivePurchaseRate > 0 THEN CostBase / rates.EffectivePurchaseRate ELSE CostBase END,
            CostAtCurrentRate = CASE WHEN rates.EffectiveCurrentRate > 0 THEN CostBase / rates.EffectiveCurrentRate ELSE CostBase END
    ) costs
)
SELECT TOP (6)
    Name,
    Subtitle,
    Revenue,
    Cost,
    Profit,
    MarginAtPurchaseRate,
    MarginAtCurrentRate,
    CurrencyMovementImpact,
    PurchaseRateToBase,
    CurrentRateToBase,
    CurrencyMovementEatsProfit = CAST(CASE
        WHEN MarginAtPurchaseRate > 0 AND MarginAtCurrentRate <= 0 THEN 1
        ELSE 0
    END AS BIT),
    CurrencyWarning = CASE
        WHEN MarginAtPurchaseRate > 0 AND MarginAtCurrentRate <= 0 THEN N'Currency movement erased profit'
        WHEN CurrencyMovementImpact < -0.01 THEN N'Currency movement reduced profit'
        ELSE N''
    END
FROM Margins
WHERE Revenue <> 0 OR Cost <> 0
ORDER BY Profit DESC, Revenue DESC, Name;";

            return session.Connection.Query<OwnerCockpitProfitRowDto>(
                sql,
                new
                {
                    BusinessDate = businessDate,
                    NextBusinessDate = nextBusinessDate,
                    SaleTypeKey = TransactionTypeKeys.Sale,
                    PurchaseTypeKey = TransactionTypeKeys.Purchase,
                    SaleReferenceType,
                    CounterRateToBase = ResolveCounterRateToBase(currencyContext),
                    currencyContext.CounterCurrencyCode
                },
                session.Transaction).ToList();
        }

        private static List<OwnerCockpitProfitRowDto> LoadProfitPerCar(DbSession session, AccountingCurrencyContext currencyContext)
        {
            const string sql = @"
WITH PartSaleRows AS
(
    SELECT
        uc.Id AS UsedCarId,
        Name = CASE
            WHEN cb.Name IS NULL OR LTRIM(RTRIM(cb.Name)) = N'' THEN CONCAT(cm.Name, N' ', uc.ModelYear)
            ELSE CONCAT(cb.Name, N' ', cm.Name, N' ', uc.ModelYear)
        END,
        SoldParts = COUNT(DISTINCT ti.PartId),
        HasWholesaleSale = CAST(0 AS INT),
        Revenue = SUM(CASE WHEN t.IsReturn = 1 THEN -COALESCE(NULLIF(ti.CounterAmount, 0), ISNULL(ti.LineTotal, 0)) ELSE COALESCE(NULLIF(ti.CounterAmount, 0), ISNULL(ti.LineTotal, 0)) END),
        Cost = MAX(COALESCE(NULLIF(uc.GrandTotalCounter, 0), ISNULL(uc.GrandTotalBase, 0) / CASE WHEN @CounterRateToBase > 0 THEN @CounterRateToBase ELSE 1 END))
    FROM dbo.Transactions t
    INNER JOIN dbo.TransactionTypes tt ON tt.Id = t.TransactionTypeId AND tt.TypeKey = @SaleTypeKey
    INNER JOIN dbo.TransactionItems ti ON ti.TransactionId = t.Id
    INNER JOIN dbo.Parts p ON p.Id = ti.PartId
    INNER JOIN dbo.UsedCars uc ON uc.Id = p.UsedCarId
    INNER JOIN dbo.CarModels cm ON cm.Id = uc.CarModelId
    LEFT JOIN dbo.CarBrands cb ON cb.Id = cm.CarBrandId
    WHERE p.UsedCarId IS NOT NULL
    GROUP BY uc.Id, cb.Name, cm.Name, uc.ModelYear
),
WholesaleRows AS
(
    SELECT
        uc.Id AS UsedCarId,
        Name = CASE
            WHEN cb.Name IS NULL OR LTRIM(RTRIM(cb.Name)) = N'' THEN CONCAT(cm.Name, N' ', uc.ModelYear)
            ELSE CONCAT(cb.Name, N' ', cm.Name, N' ', uc.ModelYear)
        END,
        SoldParts = CAST(0 AS INT),
        HasWholesaleSale = CAST(1 AS INT),
        Revenue = SUM(COALESCE(NULLIF(w.SalePriceCounter, 0), CASE WHEN rate.EffectiveCounterRate > 0 THEN ISNULL(w.SalePriceBase, 0) / rate.EffectiveCounterRate ELSE ISNULL(w.SalePrice, 0) END)),
        Cost = MAX(CASE
            WHEN rate.EffectiveCounterRate > 0 THEN (cost.FullCostBase + ISNULL(w.RepairTotalBaseAmount, 0)) / rate.EffectiveCounterRate
            ELSE cost.FullCostBase + ISNULL(w.RepairTotalBaseAmount, 0)
        END)
    FROM dbo.UsedCarWholesaleSales w
    INNER JOIN dbo.UsedCars uc ON uc.Id = w.UsedCarId
    INNER JOIN dbo.CarModels cm ON cm.Id = uc.CarModelId
    LEFT JOIN dbo.CarBrands cb ON cb.Id = cm.CarBrandId
    CROSS APPLY
    (
        SELECT EffectiveCounterRate = COALESCE(NULLIF(w.CounterRateToBase, 0), NULLIF(uc.CounterRateToBase, 0), NULLIF(@CounterRateToBase, 0), 1)
    ) rate
    CROSS APPLY
    (
        SELECT FullCostBase = CASE
            WHEN ISNULL(uc.GrandTotalBase, 0) > 0 THEN ISNULL(uc.GrandTotalBase, 0)
            ELSE ISNULL(uc.PriceBase, 0)
                + ((ISNULL(uc.Transportation, 0) + ISNULL(uc.PartOutAmount, 0) + ISNULL(uc.Shipping, 0) + ISNULL(uc.Customs, 0) + ISNULL(uc.Repairs, 0)) * rate.EffectiveCounterRate)
        END
    ) cost
    GROUP BY uc.Id, cb.Name, cm.Name, uc.ModelYear
),
CarRows AS
(
    SELECT
        UsedCarId,
        Name = MAX(Name),
        SoldParts = SUM(SoldParts),
        HasWholesaleSale = MAX(HasWholesaleSale),
        Revenue = SUM(Revenue),
        Cost = MAX(Cost)
    FROM
    (
        SELECT UsedCarId, Name, SoldParts, HasWholesaleSale, Revenue, Cost FROM PartSaleRows
        UNION ALL
        SELECT UsedCarId, Name, SoldParts, HasWholesaleSale, Revenue, Cost FROM WholesaleRows
    ) rows
    GROUP BY UsedCarId
)
SELECT TOP (6)
    Name,
    Subtitle = CASE
        WHEN HasWholesaleSale = 1 AND SoldParts > 0 THEN CONCAT(CONVERT(NVARCHAR(20), SoldParts), N' linked parts sold + wholesale sale')
        WHEN HasWholesaleSale = 1 THEN N'Wholesale used-car sale'
        ELSE CONCAT(CONVERT(NVARCHAR(20), SoldParts), N' linked parts sold')
    END,
    Revenue = ROUND(Revenue, 4),
    Cost = ROUND(Cost, 4),
    Profit = ROUND(Revenue - Cost, 4)
FROM CarRows
WHERE Revenue <> 0 OR Cost <> 0
ORDER BY Profit DESC, Revenue DESC, Name;";

            return session.Connection.Query<OwnerCockpitProfitRowDto>(
                sql,
                new
                {
                    SaleTypeKey = TransactionTypeKeys.Sale,
                    CounterRateToBase = ResolveCounterRateToBase(currencyContext)
                },
                session.Transaction).ToList();
        }

        private static List<OwnerCockpitProfitHeatmapRowDto> LoadProfitHeatmap(
            DbSession session,
            DateTime businessDate,
            DateTime nextBusinessDate,
            AccountingCurrencyContext currencyContext)
        {
            const string sql = @"
WITH PartSegments AS
(
    SELECT
        PartId = p.Id,
        CategoryId = CASE WHEN p.UsedCarId IS NULL THEN p.CategoryId ELSE NULL END,
        SegmentKey = CASE
            WHEN p.UsedCarId IS NOT NULL THEN N'used-car-parts'
            WHEN search.SearchText LIKE N'%engine%' OR search.SearchText LIKE N'%timing belt%' THEN N'engines'
            WHEN search.SearchText LIKE N'%light%' OR search.SearchText LIKE N'%lamp%' THEN N'lights'
            WHEN search.SearchText LIKE N'%bumper%' OR search.SearchText LIKE N'%body%' OR search.SearchText LIKE N'%exterior%' THEN N'bumpers'
            WHEN search.SearchText LIKE N'%electrical%' OR search.SearchText LIKE N'%electronic%'
              OR search.SearchText LIKE N'%sensor%' OR search.SearchText LIKE N'%module%'
              OR search.SearchText LIKE N'%alternator%' OR search.SearchText LIKE N'%spark plug%'
              OR search.SearchText LIKE N'% ecu %' THEN N'electronics'
            ELSE CONCAT(N'category-', CONVERT(NVARCHAR(20), p.CategoryId))
        END,
        CategoryName = CASE
            WHEN p.UsedCarId IS NOT NULL THEN N'Used-car parts'
            WHEN search.SearchText LIKE N'%engine%' OR search.SearchText LIKE N'%timing belt%' THEN N'Engines'
            WHEN search.SearchText LIKE N'%light%' OR search.SearchText LIKE N'%lamp%' THEN N'Lights'
            WHEN search.SearchText LIKE N'%bumper%' OR search.SearchText LIKE N'%body%' OR search.SearchText LIKE N'%exterior%' THEN N'Bumpers / body'
            WHEN search.SearchText LIKE N'%electrical%' OR search.SearchText LIKE N'%electronic%'
              OR search.SearchText LIKE N'%sensor%' OR search.SearchText LIKE N'%module%'
              OR search.SearchText LIKE N'%alternator%' OR search.SearchText LIKE N'%spark plug%'
              OR search.SearchText LIKE N'% ecu %' THEN N'Electronics'
            ELSE COALESCE(NULLIF(c.Name, N''), N'Uncategorized')
        END,
        UnitValueBase = COALESCE(NULLIF(p.AllocatedCost, 0), NULLIF(p.CostPrice, 0), NULLIF(p.AveragePrice, 0), 0)
    FROM dbo.Parts p
    LEFT JOIN dbo.Categories c ON c.Id = p.CategoryId
    CROSS APPLY
    (
        SELECT SearchText = LOWER(CONCAT(N' ', ISNULL(c.Name, N''), N' ', ISNULL(p.Name, N''), N' ', ISNULL(p.OEMNumber, N''), N' '))
    ) search
    WHERE p.IsActive = 1
),
StockBySegment AS
(
    SELECT
        ps.SegmentKey,
        StockUnits = ISNULL(SUM(CAST(ISNULL(s.Quantity, 0) AS DECIMAL(19, 4))), 0),
        StockValue = ROUND(ISNULL(SUM(CAST(ISNULL(s.Quantity, 0) AS DECIMAL(19, 4)) * ps.UnitValueBase), 0)
            / CASE WHEN @CounterRateToBase > 0 THEN @CounterRateToBase ELSE 1 END, 4),
        DeadStockUnits = ISNULL(SUM(CASE
            WHEN ISNULL(s.Quantity, 0) > 0
             AND (lastSale.LastSoldAt IS NULL OR lastSale.LastSoldAt < @DeadStockCutoff)
            THEN CAST(s.Quantity AS DECIMAL(19, 4)) ELSE 0 END), 0),
        DeadStockValue = ROUND(ISNULL(SUM(CASE
            WHEN ISNULL(s.Quantity, 0) > 0
             AND (lastSale.LastSoldAt IS NULL OR lastSale.LastSoldAt < @DeadStockCutoff)
            THEN CAST(s.Quantity AS DECIMAL(19, 4)) * ps.UnitValueBase ELSE 0 END), 0)
            / CASE WHEN @CounterRateToBase > 0 THEN @CounterRateToBase ELSE 1 END, 4)
    FROM PartSegments ps
    LEFT JOIN dbo.Stock s ON s.PartId = ps.PartId
    OUTER APPLY
    (
        SELECT LastSoldAt = MAX(t.TransactionDate)
        FROM dbo.Transactions t
        INNER JOIN dbo.TransactionTypes tt ON tt.Id = t.TransactionTypeId AND tt.TypeKey = @SaleTypeKey
        INNER JOIN dbo.TransactionItems ti ON ti.TransactionId = t.Id
        WHERE ti.PartId = ps.PartId
          AND t.IsReturn = 0
    ) lastSale
    GROUP BY ps.SegmentKey
),
SalesBySegment AS
(
    SELECT
        ps.SegmentKey,
        TurnoverUnits = ISNULL(SUM(CASE
            WHEN t.IsReturn = 1 THEN -ABS(ISNULL(ti.Quantity, 0))
            ELSE ABS(ISNULL(ti.Quantity, 0))
        END), 0),
        Revenue = ROUND(ISNULL(SUM(CASE
            WHEN t.IsReturn = 1 THEN -COALESCE(NULLIF(ti.CounterAmount, 0), ISNULL(ti.LineTotal, 0))
            ELSE COALESCE(NULLIF(ti.CounterAmount, 0), ISNULL(ti.LineTotal, 0))
        END), 0), 4),
        Cost = ROUND(ISNULL(SUM(CASE
            WHEN t.IsReturn = 1 THEN -COALESCE(NULLIF(m.MovementCost, 0), ABS(ISNULL(ti.Quantity, 0)) * ps.UnitValueBase)
            ELSE COALESCE(NULLIF(m.MovementCost, 0), ABS(ISNULL(ti.Quantity, 0)) * ps.UnitValueBase)
        END), 0) / CASE WHEN @CounterRateToBase > 0 THEN @CounterRateToBase ELSE 1 END, 4)
    FROM dbo.Transactions t
    INNER JOIN dbo.TransactionTypes tt ON tt.Id = t.TransactionTypeId AND tt.TypeKey = @SaleTypeKey
    INNER JOIN dbo.TransactionItems ti ON ti.TransactionId = t.Id
    INNER JOIN PartSegments ps ON ps.PartId = ti.PartId
    OUTER APPLY
    (
        SELECT SUM(ABS(CAST(sm.Quantity AS DECIMAL(19, 4))) * sm.UnitCost) AS MovementCost
        FROM dbo.StockMovements sm
        WHERE sm.ReferenceType = @SaleReferenceType
          AND sm.ReferenceId = t.ReferenceId
          AND sm.PartId = ti.PartId
    ) m
    WHERE t.TransactionDate >= @StartDate
      AND t.TransactionDate < @NextBusinessDate
      AND ti.PartId IS NOT NULL
    GROUP BY ps.SegmentKey
)
SELECT
    CategoryId = CASE WHEN COUNT(DISTINCT ps.CategoryId) = 1 THEN MAX(ps.CategoryId) ELSE NULL END,
    ps.SegmentKey,
    CategoryName = MAX(ps.CategoryName),
    Revenue = ISNULL(MAX(sales.Revenue), 0),
    Cost = ISNULL(MAX(sales.Cost), 0),
    Profit = ROUND(ISNULL(MAX(sales.Revenue), 0) - ISNULL(MAX(sales.Cost), 0), 4),
    TurnoverUnits = ISNULL(MAX(sales.TurnoverUnits), 0),
    StockUnits = ISNULL(MAX(stock.StockUnits), 0),
    StockValue = ISNULL(MAX(stock.StockValue), 0),
    DeadStockUnits = ISNULL(MAX(stock.DeadStockUnits), 0),
    DeadStockValue = ISNULL(MAX(stock.DeadStockValue), 0)
FROM PartSegments ps
LEFT JOIN SalesBySegment sales ON sales.SegmentKey = ps.SegmentKey
LEFT JOIN StockBySegment stock ON stock.SegmentKey = ps.SegmentKey
GROUP BY ps.SegmentKey
HAVING ISNULL(MAX(sales.Revenue), 0) <> 0
    OR ISNULL(MAX(sales.Cost), 0) <> 0
    OR ISNULL(MAX(stock.StockUnits), 0) <> 0
    OR ISNULL(MAX(stock.DeadStockUnits), 0) <> 0;";

            var rows = session.Connection.Query<OwnerCockpitProfitHeatmapRowDto>(
                sql,
                new
                {
                    StartDate = businessDate.AddDays(-30),
                    NextBusinessDate = nextBusinessDate,
                    DeadStockCutoff = businessDate.AddDays(-90),
                    SaleTypeKey = TransactionTypeKeys.Sale,
                    SaleReferenceType,
                    CounterRateToBase = ResolveCounterRateToBase(currencyContext)
                },
                session.Transaction).ToList();

            foreach (var row in rows)
            {
                ApplyProfitHeatmapSignals(row);
            }

            return rows
                .OrderBy(row => SignalRank(row.OverallSignal))
                .ThenBy(row => row.Profit)
                .ThenByDescending(row => row.DeadStockValue)
                .ThenByDescending(row => row.Revenue)
                .ThenBy(row => row.CategoryName)
                .Take(12)
                .ToList();
        }

        private static List<OwnerCockpitUnpaidTransactionDto> LoadUnpaidTransactions(DbSession session, AccountingCurrencyContext currencyContext)
        {
            const string sql = @"
SELECT TOP (10)
    TransactionType = CASE tt.TypeKey
        WHEN @SaleTypeKey THEN N'Sale'
        WHEN @PurchaseTypeKey THEN N'Purchase'
        WHEN @UsedCarPurchaseTypeKey THEN N'Used car purchase'
        ELSE tt.Name
    END,
    TransactionNumber = t.TransactionNumber,
    TransactionDate = t.TransactionDate,
    Counterparty = COALESCE(NULLIF(c.Name, N''), NULLIF(s.Name, N''), N'Unassigned'),
    TotalAmount = amounts.DisplayTotalAmount,
    PaidAmount = amounts.DisplayPaidAmount,
    RemainingAmount = amounts.DisplayTotalAmount - amounts.DisplayPaidAmount,
    PaymentStatus = t.PaymentStatus
FROM dbo.Transactions t
INNER JOIN dbo.TransactionTypes tt ON tt.Id = t.TransactionTypeId
LEFT JOIN dbo.Customers c ON c.Id = t.CustomerId
LEFT JOIN dbo.Suppliers s ON s.Id = t.SupplierId
OUTER APPLY
(
    SELECT
        DisplayTotalAmount = COALESCE(NULLIF(t.TotalCounterAmount, 0), CASE WHEN @CounterRateToBase > 0 THEN COALESCE(NULLIF(t.TotalBaseAmount, 0), t.TotalAmount, 0) / @CounterRateToBase ELSE t.TotalAmount END),
        DisplayPaidAmount = COALESCE(NULLIF(t.PaidCounterAmount, 0), CASE WHEN @CounterRateToBase > 0 THEN ISNULL(t.PaidAmount, 0) / @CounterRateToBase ELSE t.PaidAmount END)
) amounts
WHERE tt.TypeKey IN (@SaleTypeKey, @PurchaseTypeKey, @UsedCarPurchaseTypeKey)
  AND amounts.DisplayTotalAmount > amounts.DisplayPaidAmount
ORDER BY t.TransactionDate, t.Id;";

            return session.Connection.Query<OwnerCockpitUnpaidTransactionDto>(
                sql,
                new
                {
                    SaleTypeKey = TransactionTypeKeys.Sale,
                    PurchaseTypeKey = TransactionTypeKeys.Purchase,
                    UsedCarPurchaseTypeKey = TransactionTypeKeys.UsedCarPurchase,
                    CounterRateToBase = ResolveCounterRateToBase(currencyContext)
                },
                session.Transaction).ToList();
        }

        private static List<OwnerCockpitAlertDto> LoadAlerts(
            DbSession session,
            DateTime businessDate,
            DateTime nextBusinessDate,
            AccountingCurrencyContext currencyContext)
        {
            var alerts = new List<OwnerCockpitAlertDto>();

            var unbalancedEntries = session.Connection.ExecuteScalar<int>(
                @"
SELECT COUNT(1)
FROM
(
    SELECT je.Id
    FROM dbo.JournalEntries je
    LEFT JOIN dbo.JournalLines jl ON jl.JournalEntryId = je.Id
    GROUP BY je.Id
    HAVING ABS(ISNULL(SUM(jl.Debit), 0) - ISNULL(SUM(jl.Credit), 0)) > 0.01
) entries;",
                transaction: session.Transaction);

            if (unbalancedEntries > 0)
            {
                alerts.Add(new OwnerCockpitAlertDto
                {
                    Severity = "Critical",
                    Title = "Unbalanced journal entries",
                    Message = $"{unbalancedEntries:N0} journal entries need debit and credit review."
                });
            }

            var missingPostingSettings = CountMissingPostingSettings(session);
            var missingCounterpartyAccounts = CountMissingCounterpartyAccounts(session);

            if (missingPostingSettings > 0 || missingCounterpartyAccounts > 0)
            {
                alerts.Add(new OwnerCockpitAlertDto
                {
                    Severity = "Warning",
                    Title = "Missing account setup",
                    Message = BuildMissingAccountSetupMessage(missingPostingSettings, missingCounterpartyAccounts)
                });
            }

            var oldSupplierBalance = LoadSupplierOverdueBalance(session, businessDate, currencyContext);
            if (oldSupplierBalance.Count > 0)
            {
                alerts.Add(new OwnerCockpitAlertDto
                {
                    Severity = "Warning",
                    Title = "Old supplier balance",
                    Message = $"{oldSupplierBalance.Count:N0} unpaid supplier transactions are older than 30 days ({oldSupplierBalance.Amount:N2})."
                });
            }

            var customerOverdue = LoadCustomerOverdueBalance(session, businessDate, currencyContext);
            if (customerOverdue.Count > 0)
            {
                alerts.Add(new OwnerCockpitAlertDto
                {
                    Severity = "Warning",
                    Title = "Customer overdue",
                    Message = $"{customerOverdue.Count:N0} customer receivables are older than 30 days ({customerOverdue.Amount:N2})."
                });
            }

            var highCostCars = LoadHighCostCars(session, currencyContext);
            if (highCostCars.Count > 0)
            {
                alerts.Add(new OwnerCockpitAlertDto
                {
                    Severity = "Warning",
                    Title = "Cars with high cost",
                    Message = $"{highCostCars.Count:N0} used cars have loaded cost above realized sales by {highCostCars.Amount:N2}."
                });
            }

            var missingCurrencyRates = CountMissingCurrencyRates(session, currencyContext);
            if (missingCurrencyRates > 0)
            {
                alerts.Add(new OwnerCockpitAlertDto
                {
                    Severity = "Warning",
                    Title = "Currency rate missing",
                    Message = $"{missingCurrencyRates:N0} active currency codes are missing a positive exchange-rate snapshot."
                });
            }

            var currencyMarginRisk = LoadCurrencyMarginRisk(session, businessDate, nextBusinessDate, currencyContext);
            if (currencyMarginRisk.Count > 0)
            {
                alerts.Add(new OwnerCockpitAlertDto
                {
                    Severity = "Warning",
                    Title = "Currency movement eating profit",
                    Message = $"{currencyMarginRisk.Count:N0} sold part(s) were profitable at purchase rate but current {currencyContext.CounterCurrencyCode} rates eroded margin by {currencyMarginRisk.Amount:N2}."
                });
            }

            var unpostedTransactions = LoadUnpostedTransactions(session, currencyContext);
            if (unpostedTransactions.Count > 0)
            {
                alerts.Add(new OwnerCockpitAlertDto
                {
                    Severity = "Warning",
                    Title = "Unposted transactions",
                    Message = $"{unpostedTransactions.Count:N0} transactions are still waiting to be posted ({unpostedTransactions.Amount:N2})."
                });
            }

            var lowStockParts = session.Connection.ExecuteScalar<int>(
                @"
SELECT COUNT(1)
FROM dbo.Parts parts
OUTER APPLY
(
    SELECT SUM(CAST(stock.Quantity AS DECIMAL(19, 4))) AS OnHand
    FROM dbo.Stock stock
    WHERE stock.PartId = parts.Id
) stockSummary
WHERE parts.IsActive = 1
  AND parts.MinStock > 0
  AND ISNULL(stockSummary.OnHand, 0) <= parts.MinStock;",
                transaction: session.Transaction);

            if (lowStockParts > 0)
            {
                alerts.Add(new OwnerCockpitAlertDto
                {
                    Severity = "Warning",
                    Title = "Low stock",
                    Message = $"{lowStockParts:N0} active parts are at or below minimum stock."
                });
            }

            var negativeStockRows = session.Connection.ExecuteScalar<int>(
                "SELECT COUNT(1) FROM dbo.Stock WHERE Quantity < 0;",
                transaction: session.Transaction);

            if (negativeStockRows > 0)
            {
                alerts.Add(new OwnerCockpitAlertDto
                {
                    Severity = "Critical",
                    Title = "Negative stock",
                    Message = $"{negativeStockRows:N0} stock rows have negative quantities."
                });
            }

            return alerts;
        }

        private static int CountMissingPostingSettings(DbSession session)
            => session.Connection.ExecuteScalar<int>(
                @"
SELECT COUNT(1)
FROM dbo.AccountingPostingRoles roles
LEFT JOIN dbo.AccountingPostingSettings settings ON settings.SettingKey = roles.RoleKey
LEFT JOIN dbo.Accounts accounts ON accounts.Id = settings.AccountId
WHERE roles.IsActive = 1
  AND (settings.AccountId IS NULL OR accounts.Id IS NULL);",
                transaction: session.Transaction);

        private static int CountMissingCounterpartyAccounts(DbSession session)
            => session.Connection.ExecuteScalar<int>(
                @"
SELECT
    (SELECT COUNT(1)
     FROM dbo.Customers customers
     LEFT JOIN dbo.Accounts accounts ON accounts.Id = customers.AccountId
     WHERE customers.AccountId IS NULL OR accounts.Id IS NULL)
  + (SELECT COUNT(1)
     FROM dbo.Suppliers suppliers
     LEFT JOIN dbo.Accounts accounts ON accounts.Id = suppliers.AccountId
     WHERE suppliers.AccountId IS NULL OR accounts.Id IS NULL);",
                transaction: session.Transaction);

        private static string BuildMissingAccountSetupMessage(int missingPostingSettings, int missingCounterpartyAccounts)
        {
            var parts = new List<string>();

            if (missingPostingSettings > 0)
            {
                parts.Add($"{missingPostingSettings:N0} posting roles");
            }

            if (missingCounterpartyAccounts > 0)
            {
                parts.Add($"{missingCounterpartyAccounts:N0} customer/supplier accounts");
            }

            return $"{string.Join(" and ", parts)} need valid account setup.";
        }

        private static CountAmountRow LoadSupplierOverdueBalance(
            DbSession session,
            DateTime businessDate,
            AccountingCurrencyContext currencyContext)
            => session.Connection.QuerySingle<CountAmountRow>(
                @"
;WITH OpenTransactions AS
(
    SELECT
        t.TransactionDate,
        RemainingAmount = COALESCE(NULLIF(t.TotalCounterAmount, 0), CASE WHEN @CounterRateToBase > 0 THEN COALESCE(NULLIF(t.TotalBaseAmount, 0), t.TotalAmount, 0) / @CounterRateToBase ELSE t.TotalAmount END)
            - COALESCE(NULLIF(t.PaidCounterAmount, 0), CASE WHEN @CounterRateToBase > 0 THEN ISNULL(t.PaidAmount, 0) / @CounterRateToBase ELSE t.PaidAmount END)
    FROM dbo.Transactions t
    INNER JOIN dbo.TransactionTypes tt ON tt.Id = t.TransactionTypeId
    WHERE tt.TypeKey IN (@PurchaseTypeKey, @UsedCarPurchaseTypeKey)
)
SELECT
    Count = COUNT(1),
    Amount = ISNULL(SUM(RemainingAmount), 0)
FROM OpenTransactions
WHERE RemainingAmount > 0
  AND TransactionDate < @OverdueDate;",
                new
                {
                    PurchaseTypeKey = TransactionTypeKeys.Purchase,
                    UsedCarPurchaseTypeKey = TransactionTypeKeys.UsedCarPurchase,
                    OverdueDate = businessDate.AddDays(-30),
                    CounterRateToBase = ResolveCounterRateToBase(currencyContext)
                },
                session.Transaction);

        private static CountAmountRow LoadCustomerOverdueBalance(
            DbSession session,
            DateTime businessDate,
            AccountingCurrencyContext currencyContext)
            => session.Connection.QuerySingle<CountAmountRow>(
                @"
;WITH OpenTransactions AS
(
    SELECT
        t.TransactionDate,
        RemainingAmount = COALESCE(NULLIF(t.TotalCounterAmount, 0), CASE WHEN @CounterRateToBase > 0 THEN COALESCE(NULLIF(t.TotalBaseAmount, 0), t.TotalAmount, 0) / @CounterRateToBase ELSE t.TotalAmount END)
            - COALESCE(NULLIF(t.PaidCounterAmount, 0), CASE WHEN @CounterRateToBase > 0 THEN ISNULL(t.PaidAmount, 0) / @CounterRateToBase ELSE t.PaidAmount END)
    FROM dbo.Transactions t
    INNER JOIN dbo.TransactionTypes tt ON tt.Id = t.TransactionTypeId
    WHERE tt.TypeKey = @SaleTypeKey
)
SELECT
    Count = COUNT(1),
    Amount = ISNULL(SUM(RemainingAmount), 0)
FROM OpenTransactions
WHERE RemainingAmount > 0
  AND TransactionDate < @OverdueDate;",
                new
                {
                    SaleTypeKey = TransactionTypeKeys.Sale,
                    OverdueDate = businessDate.AddDays(-30),
                    CounterRateToBase = ResolveCounterRateToBase(currencyContext)
                },
                session.Transaction);

        private static CountAmountRow LoadHighCostCars(DbSession session, AccountingCurrencyContext currencyContext)
            => session.Connection.QuerySingle<CountAmountRow>(
                @"
;WITH SalesByCar AS
(
    SELECT
        p.UsedCarId,
        SalesRevenue = SUM(CASE WHEN t.IsReturn = 1 THEN -COALESCE(NULLIF(ti.CounterAmount, 0), ISNULL(ti.LineTotal, 0)) ELSE COALESCE(NULLIF(ti.CounterAmount, 0), ISNULL(ti.LineTotal, 0)) END),
        SaleCount = COUNT(DISTINCT t.Id)
    FROM dbo.Transactions t
    INNER JOIN dbo.TransactionTypes tt ON tt.Id = t.TransactionTypeId AND tt.TypeKey = @SaleTypeKey
    INNER JOIN dbo.TransactionItems ti ON ti.TransactionId = t.Id
    INNER JOIN dbo.Parts p ON p.Id = ti.PartId
    WHERE p.UsedCarId IS NOT NULL
    GROUP BY p.UsedCarId
),
HighCostCars AS
(
    SELECT
        CostGap = COALESCE(NULLIF(uc.GrandTotalCounter, 0), ISNULL(uc.GrandTotalBase, 0) / CASE WHEN @CounterRateToBase > 0 THEN @CounterRateToBase ELSE 1 END) - ISNULL(s.SalesRevenue, 0)
    FROM dbo.UsedCars uc
    INNER JOIN SalesByCar s ON s.UsedCarId = uc.Id
    WHERE s.SaleCount > 0
      AND ISNULL(uc.GrandTotalBase, 0) > ISNULL(s.SalesRevenue, 0)
)
SELECT
    Count = COUNT(1),
    Amount = ISNULL(SUM(CostGap), 0)
FROM HighCostCars;",
                new
                {
                    SaleTypeKey = TransactionTypeKeys.Sale,
                    CounterRateToBase = ResolveCounterRateToBase(currencyContext)
                },
                session.Transaction);

        private static int CountMissingCurrencyRates(DbSession session, AccountingCurrencyContext currencyContext)
            => session.Connection.ExecuteScalar<int>(
                @"
;WITH CurrencyCodes AS
(
    SELECT UPPER(LTRIM(RTRIM(@BaseCurrencyCode))) AS Code
    UNION
    SELECT UPPER(LTRIM(RTRIM(@CounterCurrencyCode)))
    UNION
    SELECT UPPER(LTRIM(RTRIM(CurrencyCode))) FROM dbo.TransactionTypes WHERE CurrencyCode IS NOT NULL
    UNION
    SELECT UPPER(LTRIM(RTRIM(BaseCurrencyCode))) FROM dbo.Transactions WHERE BaseCurrencyCode IS NOT NULL
    UNION
    SELECT UPPER(LTRIM(RTRIM(CounterCurrencyCode))) FROM dbo.Transactions WHERE CounterCurrencyCode IS NOT NULL
    UNION
    SELECT UPPER(LTRIM(RTRIM(CurrencyCode))) FROM dbo.TransactionItems WHERE CurrencyCode IS NOT NULL
    UNION
    SELECT UPPER(LTRIM(RTRIM(Currency))) FROM dbo.Parts WHERE Currency IS NOT NULL
    UNION
    SELECT UPPER(LTRIM(RTRIM(PriceCurrency))) FROM dbo.UsedCars WHERE PriceCurrency IS NOT NULL
    UNION
    SELECT UPPER(LTRIM(RTRIM(BaseCurrencyCode))) FROM dbo.UsedCars WHERE BaseCurrencyCode IS NOT NULL
    UNION
    SELECT UPPER(LTRIM(RTRIM(CounterCurrencyCode))) FROM dbo.UsedCars WHERE CounterCurrencyCode IS NOT NULL
    UNION
    SELECT UPPER(LTRIM(RTRIM(ShippingFeesCurrencyCode))) FROM dbo.Location WHERE ShippingFeesCurrencyCode IS NOT NULL
),
DistinctCurrencyCodes AS
(
    SELECT DISTINCT Code
    FROM CurrencyCodes
    WHERE Code IS NOT NULL
      AND LEN(Code) = 3
      AND Code <> UPPER(LTRIM(RTRIM(@BaseCurrencyCode)))
)
SELECT COUNT(1)
FROM DistinctCurrencyCodes codes
LEFT JOIN dbo.CurrencyRates rates ON UPPER(LTRIM(RTRIM(rates.Code))) = codes.Code
WHERE rates.Code IS NULL
   OR rates.RateToUsd <= 0;",
                new
                {
                    currencyContext.BaseCurrencyCode,
                    currencyContext.CounterCurrencyCode
                },
                session.Transaction);

        private static CountAmountRow LoadCurrencyMarginRisk(
            DbSession session,
            DateTime businessDate,
            DateTime nextBusinessDate,
            AccountingCurrencyContext currencyContext)
            => session.Connection.QuerySingle<CountAmountRow>(
                @"
WITH PurchaseRates AS
(
    SELECT
        ti.PartId,
        PurchaseRateToBase =
            SUM(
                ABS(ISNULL(ti.Quantity, 0))
                * COALESCE(
                    CASE
                        WHEN ABS(ISNULL(ti.CounterAmount, 0)) > 0 AND ABS(ISNULL(ti.BaseAmount, 0)) > 0
                        THEN ABS(ti.BaseAmount) / ABS(ti.CounterAmount)
                        ELSE NULL
                    END,
                    NULLIF(ti.RateToBase, 0),
                    CASE
                        WHEN ABS(ISNULL(t.TotalCounterAmount, 0)) > 0 AND ABS(ISNULL(t.TotalBaseAmount, 0)) > 0
                        THEN ABS(t.TotalBaseAmount) / ABS(t.TotalCounterAmount)
                        ELSE NULL
                    END,
                    NULLIF(@CounterRateToBase, 0),
                    1
                )
            ) / NULLIF(SUM(ABS(ISNULL(ti.Quantity, 0))), 0)
    FROM dbo.Transactions t
    INNER JOIN dbo.TransactionTypes tt ON tt.Id = t.TransactionTypeId AND tt.TypeKey = @PurchaseTypeKey
    INNER JOIN dbo.TransactionItems ti ON ti.TransactionId = t.Id
    WHERE ti.PartId IS NOT NULL
      AND UPPER(LTRIM(RTRIM(ISNULL(NULLIF(t.CounterCurrencyCode, N''), @CounterCurrencyCode)))) = UPPER(LTRIM(RTRIM(@CounterCurrencyCode)))
    GROUP BY ti.PartId
),
LineRows AS
(
    SELECT
        p.Id AS PartId,
        Revenue = SUM(CASE
            WHEN t.IsReturn = 1 THEN -COALESCE(NULLIF(ti.CounterAmount, 0), ISNULL(ti.LineTotal, 0))
            ELSE COALESCE(NULLIF(ti.CounterAmount, 0), ISNULL(ti.LineTotal, 0))
        END),
        CostBase = SUM(CASE
            WHEN t.IsReturn = 1 THEN -COALESCE(NULLIF(m.MovementCost, 0), ABS(ISNULL(ti.Quantity, 0)) * COALESCE(NULLIF(p.AllocatedCost, 0), NULLIF(p.CostPrice, 0), NULLIF(p.AveragePrice, 0), 0))
            ELSE COALESCE(NULLIF(m.MovementCost, 0), ABS(ISNULL(ti.Quantity, 0)) * COALESCE(NULLIF(p.AllocatedCost, 0), NULLIF(p.CostPrice, 0), NULLIF(p.AveragePrice, 0), 0))
        END),
        PurchaseRateToBase = COALESCE(
            NULLIF(MAX(CASE
                WHEN uc.Id IS NOT NULL
                 AND UPPER(LTRIM(RTRIM(ISNULL(NULLIF(uc.CounterCurrencyCode, N''), @CounterCurrencyCode)))) = UPPER(LTRIM(RTRIM(@CounterCurrencyCode)))
                THEN uc.CounterRateToBase
                ELSE NULL
            END), 0),
            NULLIF(MAX(pr.PurchaseRateToBase), 0),
            NULLIF(MAX(CASE WHEN ISNULL(ti.RateToBase, 0) > 0 THEN ti.RateToBase ELSE NULL END), 0),
            NULLIF(@CounterRateToBase, 0),
            1)
    FROM dbo.Transactions t
    INNER JOIN dbo.TransactionTypes tt ON tt.Id = t.TransactionTypeId AND tt.TypeKey = @SaleTypeKey
    INNER JOIN dbo.TransactionItems ti ON ti.TransactionId = t.Id
    INNER JOIN dbo.Parts p ON p.Id = ti.PartId
    LEFT JOIN dbo.UsedCars uc ON uc.Id = p.UsedCarId
    LEFT JOIN PurchaseRates pr ON pr.PartId = p.Id
    OUTER APPLY
    (
        SELECT SUM(ABS(CAST(sm.Quantity AS DECIMAL(19, 4))) * sm.UnitCost) AS MovementCost
        FROM dbo.StockMovements sm
        WHERE sm.ReferenceType = @SaleReferenceType
          AND sm.ReferenceId = t.ReferenceId
          AND sm.PartId = ti.PartId
    ) m
    WHERE t.TransactionDate >= @BusinessDate
      AND t.TransactionDate < @NextBusinessDate
      AND ti.PartId IS NOT NULL
    GROUP BY p.Id
),
Margins AS
(
    SELECT
        MarginAtPurchaseRate = Revenue - CostAtPurchaseRate,
        MarginAtCurrentRate = Revenue - CostAtCurrentRate
    FROM LineRows
    CROSS APPLY
    (
        SELECT
            EffectivePurchaseRate = CASE WHEN PurchaseRateToBase > 0 THEN PurchaseRateToBase ELSE 1 END,
            EffectiveCurrentRate = CASE WHEN @CounterRateToBase > 0 THEN @CounterRateToBase ELSE 1 END
    ) rates
    CROSS APPLY
    (
        SELECT
            CostAtPurchaseRate = CASE WHEN rates.EffectivePurchaseRate > 0 THEN CostBase / rates.EffectivePurchaseRate ELSE CostBase END,
            CostAtCurrentRate = CASE WHEN rates.EffectiveCurrentRate > 0 THEN CostBase / rates.EffectiveCurrentRate ELSE CostBase END
    ) costs
)
SELECT
    Count = COUNT(1),
    Amount = ISNULL(SUM(MarginAtPurchaseRate - MarginAtCurrentRate), 0)
FROM Margins
WHERE MarginAtPurchaseRate > 0
  AND MarginAtCurrentRate <= 0;",
                new
                {
                    BusinessDate = businessDate,
                    NextBusinessDate = nextBusinessDate,
                    SaleTypeKey = TransactionTypeKeys.Sale,
                    PurchaseTypeKey = TransactionTypeKeys.Purchase,
                    SaleReferenceType,
                    CounterRateToBase = ResolveCounterRateToBase(currencyContext),
                    currencyContext.CounterCurrencyCode
                },
                session.Transaction);

        private static CountAmountRow LoadUnpostedTransactions(DbSession session, AccountingCurrencyContext currencyContext)
            => session.Connection.QuerySingle<CountAmountRow>(
                @"
;WITH DraftTransactions AS
(
    SELECT
        Amount = COALESCE(NULLIF(t.TotalCounterAmount, 0), CASE WHEN @CounterRateToBase > 0 THEN COALESCE(NULLIF(t.TotalBaseAmount, 0), t.TotalAmount, 0) / @CounterRateToBase ELSE t.TotalAmount END)
    FROM dbo.Transactions t
    INNER JOIN dbo.TransactionTypes tt ON tt.Id = t.TransactionTypeId
    WHERE tt.TypeKey IN (@SaleTypeKey, @PurchaseTypeKey, @UsedCarPurchaseTypeKey)
      AND NULLIF(LTRIM(RTRIM(t.PostingStatus)), N'') IS NOT NULL
      AND UPPER(LTRIM(RTRIM(t.PostingStatus))) <> N'POSTED'
)
SELECT
    Count = COUNT(1),
    Amount = ISNULL(SUM(Amount), 0)
FROM DraftTransactions;",
                new
                {
                    SaleTypeKey = TransactionTypeKeys.Sale,
                    PurchaseTypeKey = TransactionTypeKeys.Purchase,
                    UsedCarPurchaseTypeKey = TransactionTypeKeys.UsedCarPurchase,
                    CounterRateToBase = ResolveCounterRateToBase(currencyContext)
                },
                session.Transaction);

        private static decimal ConvertBaseToCounter(decimal baseAmount, AccountingCurrencyContext currencyContext)
        {
            var rate = ResolveCounterRateToBase(currencyContext);
            if (rate <= 0m || string.Equals(currencyContext.BaseCurrencyCode, currencyContext.CounterCurrencyCode, StringComparison.OrdinalIgnoreCase))
            {
                return baseAmount;
            }

            return decimal.Round(baseAmount / rate, 4, MidpointRounding.AwayFromZero);
        }

        private static decimal ResolveCounterRateToBase(AccountingCurrencyContext currencyContext)
            => currencyContext.CounterRateToBase > 0m
                ? currencyContext.CounterRateToBase
                : 1m;

        private static void ApplyProfitHeatmapSignals(OwnerCockpitProfitHeatmapRowDto row)
        {
            var revenue = Math.Abs(row.Revenue);
            var liveStockUnits = Math.Max(row.StockUnits, 0m);
            var liveTurnoverUnits = Math.Max(row.TurnoverUnits, 0m);

            row.ProfitMarginPercent = revenue <= 0m
                ? 0m
                : decimal.Round(row.Profit / revenue * 100m, 1, MidpointRounding.AwayFromZero);

            var turnoverBase = liveTurnoverUnits + liveStockUnits;
            row.TurnoverRatePercent = turnoverBase <= 0m
                ? 0m
                : decimal.Round(liveTurnoverUnits / turnoverBase * 100m, 1, MidpointRounding.AwayFromZero);

            row.DeadStockPercent = liveStockUnits <= 0m
                ? 0m
                : decimal.Round(row.DeadStockUnits / liveStockUnits * 100m, 1, MidpointRounding.AwayFromZero);

            row.ProfitSignal = ResolveProfitSignal(row);
            row.TurnoverSignal = ResolveTurnoverSignal(row.TurnoverRatePercent);
            row.DeadStockSignal = ResolveDeadStockSignal(row.StockUnits, row.DeadStockPercent);
            row.Score = HeatmapSignalScore(row.ProfitSignal, 45)
                + HeatmapSignalScore(row.TurnoverSignal, 35)
                + HeatmapSignalScore(row.DeadStockSignal, 20);
            row.OverallSignal = ResolveOverallSignal(row);
        }

        private static string ResolveProfitSignal(OwnerCockpitProfitHeatmapRowDto row)
        {
            if (row.Profit < 0m || (row.Revenue > 0m && row.ProfitMarginPercent < 8m))
            {
                return "Red";
            }

            if (row.Profit > 0m && row.ProfitMarginPercent >= 22m)
            {
                return "Green";
            }

            return "Yellow";
        }

        private static string ResolveTurnoverSignal(decimal turnoverRatePercent)
        {
            if (turnoverRatePercent >= 35m)
            {
                return "Green";
            }

            return turnoverRatePercent >= 12m ? "Yellow" : "Red";
        }

        private static string ResolveDeadStockSignal(decimal stockUnits, decimal deadStockPercent)
        {
            if (stockUnits <= 0m || deadStockPercent <= 10m)
            {
                return "Green";
            }

            return deadStockPercent <= 30m ? "Yellow" : "Red";
        }

        private static int HeatmapSignalScore(string signal, int weight)
            => signal switch
            {
                "Green" => weight,
                "Yellow" => weight / 2,
                _ => Math.Max(0, weight / 8)
            };

        private static string ResolveOverallSignal(OwnerCockpitProfitHeatmapRowDto row)
        {
            if ((row.ProfitSignal == "Red" && row.Profit < 0m) ||
                (row.TurnoverSignal == "Red" && row.DeadStockSignal == "Red"))
            {
                return "Red";
            }

            return row.Score >= 70
                ? "Green"
                : row.Score >= 45
                    ? "Yellow"
                    : "Red";
        }

        private static int SignalRank(string signal)
            => signal switch
            {
                "Red" => 0,
                "Yellow" => 1,
                "Green" => 2,
                _ => 3
            };

        private static int ExpenseCategoryRank(string category)
            => category switch
            {
                OwnerCockpitDailyProfitLossCalculator.RentCategory => 0,
                OwnerCockpitDailyProfitLossCalculator.LaborCategory => 1,
                OwnerCockpitDailyProfitLossCalculator.OtherCategory => 2,
                _ => 3
            };

    }
}
