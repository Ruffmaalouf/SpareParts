using Dapper;
using SpareParts.Domain.MasterData;
using SpareParts.Domain.Reports;
using System.Data;
using System.Globalization;

namespace SpareParts.Infrastructure.Services;

public sealed partial class ReportBuilderService
{
    private ReportCurrencyProjectionContext ResolveCurrencyProjectionContext(
        IDbConnection connection,
        ReportCurrencySettingsDto? settings,
        IReadOnlyList<ReportColumnDto> availableColumns,
        IReadOnlyDictionary<string, string> aliasByTableKey)
    {
        if (settings == null || !settings.IsEnabled || string.IsNullOrWhiteSpace(settings.AmountColumnKey))
        {
            return new ReportCurrencyProjectionContext();
        }

        var columnsByKey = availableColumns.ToDictionary(column => column.Key, StringComparer.OrdinalIgnoreCase);
        var columnsByQualifiedName = availableColumns.ToDictionary(column => column.QualifiedColumnName, StringComparer.OrdinalIgnoreCase);
        var uniqueColumnsByName = availableColumns
            .GroupBy(column => column.ColumnName, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() == 1)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        var amountColumn = ResolveColumnReference(
            settings.AmountColumnKey,
            settings.AmountColumnKey,
            columnsByKey,
            columnsByQualifiedName,
            uniqueColumnsByName,
            "currency amount");

        var (configuredBaseCurrencyCode, configuredCounterCurrencyCode) = GetConfiguredCurrencyContext(connection, _tenantContext.TenantId);
        var reportingCurrencyCode = NormalizeCurrencyCode(settings.ReportingCurrencyCode) ?? configuredBaseCurrencyCode;

        var amountExpression = $"{aliasByTableKey[amountColumn.TableKey]}.{Quote(amountColumn.ColumnName)}";
        var rateExpression = TryResolveOptionalColumnExpression(settings.RateToBaseColumnKey, columnsByKey, columnsByQualifiedName, uniqueColumnsByName, aliasByTableKey)
            ?? "1";
        var transactionCurrencyExpression = TryResolveOptionalColumnExpression(settings.CurrencyCodeColumnKey, columnsByKey, columnsByQualifiedName, uniqueColumnsByName, aliasByTableKey)
            ?? $"N'{configuredBaseCurrencyCode}'";
        var baseCurrencyExpression = TryResolveOptionalColumnExpression(settings.BaseCurrencyCodeColumnKey, columnsByKey, columnsByQualifiedName, uniqueColumnsByName, aliasByTableKey)
            ?? $"N'{configuredBaseCurrencyCode}'";
        var counterCurrencyExpression = TryResolveOptionalColumnExpression(settings.CounterCurrencyCodeColumnKey, columnsByKey, columnsByQualifiedName, uniqueColumnsByName, aliasByTableKey)
            ?? $"N'{configuredCounterCurrencyCode}'";

        var counterRateExpression = TryResolveOptionalColumnExpression(settings.CounterRateToBaseColumnKey, columnsByKey, columnsByQualifiedName, uniqueColumnsByName, aliasByTableKey);
        if (string.IsNullOrWhiteSpace(counterRateExpression))
        {
            var configuredCounterRateToBase = ResolveCurrencyRateToConfiguredBase(connection, configuredCounterCurrencyCode, configuredBaseCurrencyCode, _tenantContext.TenantId) ?? 1m;
            counterRateExpression = configuredCounterRateToBase.ToString(CultureInfo.InvariantCulture);
        }

        var baseAmountExpression = $"ROUND(({amountExpression}) * (CASE WHEN ISNULL({rateExpression}, 0) > 0 THEN {rateExpression} ELSE 1 END), 4)";
        var counterAmountExpression = $"CASE WHEN ISNULL({counterRateExpression}, 0) > 0 THEN ROUND(({baseAmountExpression}) / {counterRateExpression}, 4) ELSE NULL END";

        var reportingRateToBase = ResolveCurrencyRateToConfiguredBase(connection, reportingCurrencyCode, configuredBaseCurrencyCode, _tenantContext.TenantId) ?? 1m;
        var reportingAmountExpression = string.Equals(reportingCurrencyCode, configuredBaseCurrencyCode, StringComparison.OrdinalIgnoreCase)
            ? baseAmountExpression
            : $"CASE WHEN {reportingRateToBase.ToString(CultureInfo.InvariantCulture)} > 0 THEN ROUND(({baseAmountExpression}) / {reportingRateToBase.ToString(CultureInfo.InvariantCulture)}, 4) ELSE NULL END";

        return new ReportCurrencyProjectionContext
        {
            IsEnabled = true,
            TransactionCurrencyExpression = transactionCurrencyExpression,
            BaseCurrencyCodeExpression = baseCurrencyExpression,
            CounterCurrencyCodeExpression = counterCurrencyExpression,
            BaseAmountExpression = baseAmountExpression,
            CounterAmountExpression = counterAmountExpression,
            ReportingAmountExpression = reportingAmountExpression,
            ReportingCurrencyCode = reportingCurrencyCode
        };
    }

    private static string? TryResolveOptionalColumnExpression(
        string? identifier,
        IReadOnlyDictionary<string, ReportColumnDto> columnsByKey,
        IReadOnlyDictionary<string, ReportColumnDto> columnsByQualifiedName,
        IReadOnlyDictionary<string, ReportColumnDto> uniqueColumnsByName,
        IReadOnlyDictionary<string, string> aliasByTableKey)
    {
        if (string.IsNullOrWhiteSpace(identifier))
        {
            return null;
        }

        var column = ResolveColumnReference(identifier, identifier, columnsByKey, columnsByQualifiedName, uniqueColumnsByName, "currency");
        return $"{aliasByTableKey[column.TableKey]}.{Quote(column.ColumnName)}";
    }

    private static decimal? ResolveCurrencyRateToConfiguredBase(IDbConnection connection, string currencyCode, string configuredBaseCode, int tenantId)
    {
        if (string.IsNullOrWhiteSpace(currencyCode) || string.IsNullOrWhiteSpace(configuredBaseCode))
        {
            return null;
        }

        if (string.Equals(currencyCode, configuredBaseCode, StringComparison.OrdinalIgnoreCase))
        {
            return 1m;
        }

        if (!TableExists(connection, "dbo", "CurrencyRates"))
        {
            return null;
        }

        var rawRates = connection.Query<CurrencyRateDto>(
                @"SELECT Code, RateToUsd, BaseCode, SnapshotUtc
                  FROM dbo.CurrencyRates
                  WHERE Code IS NOT NULL
                    AND BaseCode IS NOT NULL
                    AND (@TenantId = 0 OR TenantId = @TenantId);",
                new { TenantId = tenantId })
            .ToList();

        if (rawRates.Count == 0)
        {
            return null;
        }

        var rawRatesByCode = rawRates
            .Select(rate => new { Rate = rate, Code = NormalizeCurrencyCode(rate.Code) })
            .Where(item => item.Code is not null)
            .GroupBy(item => item.Code!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Last().Rate, StringComparer.OrdinalIgnoreCase);

        var unitsPerReferenceCurrency = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        var configuredBaseUnits = ResolveUnitsPerReferenceCurrency(configuredBaseCode, rawRatesByCode, unitsPerReferenceCurrency, []);
        var currencyUnits = ResolveUnitsPerReferenceCurrency(currencyCode, rawRatesByCode, unitsPerReferenceCurrency, []);

        if (configuredBaseUnits <= 0 || currencyUnits <= 0)
        {
            return null;
        }

        return decimal.Round(currencyUnits / configuredBaseUnits, 8, MidpointRounding.AwayFromZero);
    }

    private static decimal ResolveUnitsPerReferenceCurrency(
        string currencyCode,
        IReadOnlyDictionary<string, CurrencyRateDto> rawRatesByCode,
        IDictionary<string, decimal> unitsPerReferenceCurrency,
        HashSet<string> activeStack)
    {
        if (unitsPerReferenceCurrency.TryGetValue(currencyCode, out var cachedUnits))
        {
            return cachedUnits;
        }

        if (!rawRatesByCode.TryGetValue(currencyCode, out var rate))
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
            var baseUnits = ResolveUnitsPerReferenceCurrency(baseCode, rawRatesByCode, unitsPerReferenceCurrency, activeStack);
            resolvedUnits = baseUnits > 0 && rate.RateToUsd > 0
                ? decimal.Round(rate.RateToUsd * baseUnits, 12, MidpointRounding.AwayFromZero)
                : 0m;
        }

        activeStack.Remove(currencyCode);
        unitsPerReferenceCurrency[currencyCode] = resolvedUnits;
        return resolvedUnits;
    }
}
