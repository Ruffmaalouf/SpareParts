using Dapper;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Api.Infrastructure;

public static class AccountingCurrencyRateRepairMigration
{
    public static void EnsureApplied(ISqlConnectionFactory factory)
    {
        using var conn = factory.CreateConnection();
        var context = ResolveCurrencyContext(conn);
        if (context.CounterRateToBase <= 0m
            || string.Equals(context.BaseCurrencyCode, context.CounterCurrencyCode, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        conn.Execute(
            @"
IF OBJECT_ID('dbo.JournalLines', 'U') IS NOT NULL
   AND OBJECT_ID('dbo.JournalEntries', 'U') IS NOT NULL
BEGIN
    ;WITH CandidateLines AS
    (
        SELECT jl.Id,
               Amount = ROUND(CASE
                   WHEN ISNULL(jl.CounterAmount, 0) > 0 THEN jl.CounterAmount
                   WHEN ISNULL(jl.OriginalAmount, 0) > 0 THEN jl.OriginalAmount
                   WHEN ISNULL(jl.Debit, 0) > 0 THEN jl.Debit
                   ELSE jl.Credit
               END, 4)
        FROM dbo.JournalLines jl
        INNER JOIN dbo.JournalEntries je ON je.Id = jl.JournalEntryId
        WHERE (
              (
                  je.ReferenceType IN (N'Sale', N'SalePayment')
                  AND UPPER(LTRIM(RTRIM(ISNULL(jl.BaseCurrencyCode, N'')))) = @BaseCurrencyCode
                  AND UPPER(LTRIM(RTRIM(ISNULL(jl.CounterCurrencyCode, N'')))) = @CounterCurrencyCode
              )
              OR
              (
                  je.ReferenceType = N'Manual'
                  AND (
                      UPPER(LTRIM(RTRIM(ISNULL(jl.BaseCurrencyCode, N'')))) <> @BaseCurrencyCode
                      OR UPPER(LTRIM(RTRIM(ISNULL(jl.CounterCurrencyCode, N'')))) <> @CounterCurrencyCode
                      OR UPPER(LTRIM(RTRIM(ISNULL(jl.BaseCurrencyCode, N'')))) = UPPER(LTRIM(RTRIM(ISNULL(jl.CounterCurrencyCode, N''))))
                  )
              )
          )
          AND UPPER(LTRIM(RTRIM(ISNULL(jl.CurrencyCode, N'')))) = @CounterCurrencyCode
          AND ROUND(ISNULL(jl.RateToBase, 0), 8) = 1
          AND (
              (ISNULL(jl.Debit, 0) > 0 AND ABS(ISNULL(jl.Debit, 0) - COALESCE(NULLIF(jl.CounterAmount, 0), NULLIF(jl.OriginalAmount, 0), jl.Debit)) <= 0.01)
              OR (ISNULL(jl.Credit, 0) > 0 AND ABS(ISNULL(jl.Credit, 0) - COALESCE(NULLIF(jl.CounterAmount, 0), NULLIF(jl.OriginalAmount, 0), jl.Credit)) <= 0.01)
          )
    )
    UPDATE jl
       SET Debit = CASE WHEN jl.Debit > 0 THEN ROUND(c.Amount * @CounterRateToBase, 4) ELSE 0 END,
           Credit = CASE WHEN jl.Credit > 0 THEN ROUND(c.Amount * @CounterRateToBase, 4) ELSE 0 END,
           CurrencyCode = @CounterCurrencyCode,
           OriginalAmount = c.Amount,
           RateToBase = @CounterRateToBase,
           CounterAmount = c.Amount,
           BaseCurrencyCode = @BaseCurrencyCode,
           CounterCurrencyCode = @CounterCurrencyCode
    FROM dbo.JournalLines jl
    INNER JOIN CandidateLines c ON c.Id = jl.Id;
END;

IF OBJECT_ID('dbo.Transactions', 'U') IS NOT NULL
   AND OBJECT_ID('dbo.TransactionTypes', 'U') IS NOT NULL
BEGIN
    UPDATE t
       SET TotalBaseAmount = ROUND(COALESCE(NULLIF(t.TotalCounterAmount, 0), t.TotalAmount, 0) * @CounterRateToBase, 4),
           TotalCounterAmount = COALESCE(NULLIF(t.TotalCounterAmount, 0), t.TotalAmount, 0),
           CounterCurrencyCode = @CounterCurrencyCode,
           BaseCurrencyCode = @BaseCurrencyCode
    FROM dbo.Transactions t
    INNER JOIN dbo.TransactionTypes tt ON tt.Id = t.TransactionTypeId
    WHERE tt.TypeKey = N'sale'
      AND UPPER(LTRIM(RTRIM(ISNULL(t.BaseCurrencyCode, N'')))) = @BaseCurrencyCode
      AND UPPER(LTRIM(RTRIM(ISNULL(t.CounterCurrencyCode, N'')))) = @CounterCurrencyCode
      AND ISNULL(t.TotalCounterAmount, 0) > 0
      AND ABS(ISNULL(t.TotalBaseAmount, 0) - ISNULL(t.TotalCounterAmount, 0)) <= 0.01;
END;

IF OBJECT_ID('dbo.TransactionItems', 'U') IS NOT NULL
   AND OBJECT_ID('dbo.Transactions', 'U') IS NOT NULL
   AND OBJECT_ID('dbo.TransactionTypes', 'U') IS NOT NULL
BEGIN
    UPDATE ti
       SET RateToBase = @CounterRateToBase,
           BaseAmount = ROUND(COALESCE(NULLIF(ti.CounterAmount, 0), ti.LineTotal, 0) * @CounterRateToBase, 4),
           CounterAmount = COALESCE(NULLIF(ti.CounterAmount, 0), ti.LineTotal, 0),
           CurrencyCode = @CounterCurrencyCode
    FROM dbo.TransactionItems ti
    INNER JOIN dbo.Transactions t ON t.Id = ti.TransactionId
    INNER JOIN dbo.TransactionTypes tt ON tt.Id = t.TransactionTypeId
    WHERE tt.TypeKey = N'sale'
      AND UPPER(LTRIM(RTRIM(ISNULL(t.BaseCurrencyCode, N'')))) = @BaseCurrencyCode
      AND UPPER(LTRIM(RTRIM(ISNULL(t.CounterCurrencyCode, N'')))) = @CounterCurrencyCode
      AND UPPER(LTRIM(RTRIM(ISNULL(ti.CurrencyCode, N'')))) = @CounterCurrencyCode
      AND ROUND(ISNULL(ti.RateToBase, 0), 8) = 1
      AND ABS(ISNULL(ti.BaseAmount, 0) - COALESCE(NULLIF(ti.CounterAmount, 0), ti.LineTotal, 0)) <= 0.01;
END;",
            new
            {
                context.BaseCurrencyCode,
                context.CounterCurrencyCode,
                context.CounterRateToBase
            });
    }

    private static CurrencyRepairContext ResolveCurrencyContext(System.Data.IDbConnection connection)
    {
        var constants = TableExists(connection, "dbo", "AppConstants")
            ? connection.Query<CurrencyRateRepairAppConstantRow>("SELECT [Key], [Value] FROM dbo.AppConstants;")
                .ToDictionary(item => item.Key, item => item.Value, StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var baseCurrencyCode = ResolveCurrencyCode(constants, "BaseCurrencyCode")
            ?? ResolveCurrencyCode(constants, "DefaultCurrencyCode")
            ?? "USD";
        var counterCurrencyCode = ResolveCurrencyCode(constants, "CounterCurrencyCode")
            ?? baseCurrencyCode;
        var configuredCounterRateToBase = constants.TryGetValue("DefaultCounterRate", out var rawCounterRate)
            && decimal.TryParse(rawCounterRate, out var parsedCounterRate)
            && parsedCounterRate > 0m
                ? decimal.Round(parsedCounterRate, 8, MidpointRounding.AwayFromZero)
                : 1m;
        var rates = LoadCurrencyRates(connection);
        var counterRateToBase = ResolveRateToBaseCurrency(rates, baseCurrencyCode, counterCurrencyCode)
            ?? configuredCounterRateToBase;

        return new CurrencyRepairContext(baseCurrencyCode, counterCurrencyCode, counterRateToBase);
    }

    private static IReadOnlyDictionary<string, CurrencyRateRepairRateRow> LoadCurrencyRates(System.Data.IDbConnection connection)
    {
        if (!TableExists(connection, "dbo", "CurrencyRates"))
        {
            return new Dictionary<string, CurrencyRateRepairRateRow>(StringComparer.OrdinalIgnoreCase);
        }

        return connection.Query<CurrencyRateRepairRateRow>(
                @"SELECT Code, RateToUsd, BaseCode
                  FROM dbo.CurrencyRates
                  WHERE Code IS NOT NULL
                    AND BaseCode IS NOT NULL;")
            .Select(rate => new { Rate = rate, Code = NormalizeCurrencyCode(rate.Code) })
            .Where(item => item.Code is not null && item.Rate.RateToUsd > 0m)
            .GroupBy(item => item.Code!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Last().Rate, StringComparer.OrdinalIgnoreCase);
    }

    private static decimal? ResolveRateToBaseCurrency(
        IReadOnlyDictionary<string, CurrencyRateRepairRateRow> ratesByCode,
        string baseCurrencyCode,
        string currencyCode)
    {
        var normalizedBaseCode = NormalizeCurrencyCode(baseCurrencyCode) ?? "USD";
        var normalizedCurrencyCode = NormalizeCurrencyCode(currencyCode);
        if (normalizedCurrencyCode is null)
        {
            return null;
        }

        if (string.Equals(normalizedCurrencyCode, normalizedBaseCode, StringComparison.OrdinalIgnoreCase))
        {
            return 1m;
        }

        var unitsPerReferenceCurrency = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        var baseUnits = ResolveUnitsPerReferenceCurrency(normalizedBaseCode, ratesByCode, unitsPerReferenceCurrency, []);
        var currencyUnits = ResolveUnitsPerReferenceCurrency(normalizedCurrencyCode, ratesByCode, unitsPerReferenceCurrency, []);
        if (baseUnits <= 0m || currencyUnits <= 0m)
        {
            return null;
        }

        return decimal.Round(baseUnits / currencyUnits, 8, MidpointRounding.AwayFromZero);
    }

    private static decimal ResolveUnitsPerReferenceCurrency(
        string currencyCode,
        IReadOnlyDictionary<string, CurrencyRateRepairRateRow> ratesByCode,
        IDictionary<string, decimal> unitsPerReferenceCurrency,
        HashSet<string> activeStack)
    {
        if (unitsPerReferenceCurrency.TryGetValue(currencyCode, out var cachedUnits))
        {
            return cachedUnits;
        }

        if (!ratesByCode.TryGetValue(currencyCode, out var rate))
        {
            unitsPerReferenceCurrency[currencyCode] = 1m;
            return 1m;
        }

        if (!activeStack.Add(currencyCode))
        {
            return 0m;
        }

        var baseCode = NormalizeCurrencyCode(rate.BaseCode) ?? currencyCode;
        decimal resolvedUnits;
        if (string.Equals(currencyCode, baseCode, StringComparison.OrdinalIgnoreCase))
        {
            resolvedUnits = 1m;
        }
        else
        {
            var baseUnits = ResolveUnitsPerReferenceCurrency(baseCode, ratesByCode, unitsPerReferenceCurrency, activeStack);
            resolvedUnits = baseUnits > 0m && rate.RateToUsd > 0m
                ? decimal.Round(rate.RateToUsd * baseUnits, 12, MidpointRounding.AwayFromZero)
                : 0m;
        }

        activeStack.Remove(currencyCode);
        unitsPerReferenceCurrency[currencyCode] = resolvedUnits;
        return resolvedUnits;
    }

    private static bool TableExists(System.Data.IDbConnection connection, string schema, string table)
        => connection.QuerySingle<int>(
            @"SELECT CASE WHEN OBJECT_ID(@QualifiedName, 'U') IS NULL THEN 0 ELSE 1 END;",
            new { QualifiedName = $"{schema}.{table}" }) == 1;

    private static string? ResolveCurrencyCode(IReadOnlyDictionary<string, string> values, string key)
    {
        if (!values.TryGetValue(key, out var value))
        {
            return null;
        }

        return NormalizeCurrencyCode(value);
    }

    private static string? NormalizeCurrencyCode(string? currencyCode)
    {
        if (string.IsNullOrWhiteSpace(currencyCode))
        {
            return null;
        }

        var normalized = currencyCode.Trim().ToUpperInvariant();
        return normalized.Length == 3 ? normalized : null;
    }
}
