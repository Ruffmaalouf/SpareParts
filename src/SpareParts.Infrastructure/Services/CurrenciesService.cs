using Dapper;
using SpareParts.Domain.MasterData;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Infrastructure.Services;

public sealed class CurrenciesService
{
    private readonly ISqlConnectionFactory _factory;

    public CurrenciesService(ISqlConnectionFactory factory)
    {
        _factory = factory;
    }

    public IEnumerable<CurrencyRateDto> GetAll()
    {
        using var conn = _factory.CreateConnection();
        var configuredBaseCode = ResolveConfiguredBaseCurrency(conn);
        var rawRates = conn.Query<CurrencyRateDto>(
            @"SELECT Code, RateToUsd, BaseCode, SnapshotUtc
              FROM CurrencyRates
              ORDER BY Code;")
            .ToList();

        if (rawRates.Count == 0)
        {
            return [];
        }

        var normalizedBaseCode = NormalizeCurrencyCode(configuredBaseCode) ?? "USD";
        var rawRatesByCode = rawRates
            .Select(rate => new { Rate = rate, Code = NormalizeCurrencyCode(rate.Code) })
            .Where(item => item.Code is not null)
            .GroupBy(item => item.Code!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Last().Rate, StringComparer.OrdinalIgnoreCase);

        var unitsPerReferenceCurrency = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        var baseUnits = ResolveUnitsPerReferenceCurrency(normalizedBaseCode, rawRatesByCode, unitsPerReferenceCurrency, []);
        if (baseUnits <= 0)
        {
            baseUnits = 1m;
        }

        var transformedRates = rawRatesByCode.Values
            .Select(rate => BuildConfiguredRate(rate, normalizedBaseCode, rawRatesByCode, unitsPerReferenceCurrency, baseUnits))
            .Where(rate => rate is not null)
            .Cast<CurrencyRateDto>()
            .OrderBy(rate => rate.Code, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (!transformedRates.Any(rate => string.Equals(rate.Code, normalizedBaseCode, StringComparison.OrdinalIgnoreCase)))
        {
            transformedRates.Insert(0, new CurrencyRateDto
            {
                Code = normalizedBaseCode,
                RateToUsd = 1m,
                BaseCode = normalizedBaseCode,
                SnapshotUtc = rawRates.Max(rate => rate.SnapshotUtc)
            });
        }

        return transformedRates;
    }

    private static CurrencyRateDto? BuildConfiguredRate(
        CurrencyRateDto rawRate,
        string configuredBaseCode,
        IReadOnlyDictionary<string, CurrencyRateDto> rawRatesByCode,
        IDictionary<string, decimal> unitsPerReferenceCurrency,
        decimal baseUnits)
    {
        var code = NormalizeCurrencyCode(rawRate.Code);
        if (code is null)
        {
            return null;
        }

        var codeUnits = ResolveUnitsPerReferenceCurrency(code, rawRatesByCode, unitsPerReferenceCurrency, []);
        if (codeUnits <= 0 || baseUnits <= 0)
        {
            return null;
        }

        return new CurrencyRateDto
        {
            Code = code,
            RateToUsd = string.Equals(code, configuredBaseCode, StringComparison.OrdinalIgnoreCase)
                ? 1m
                : decimal.Round(codeUnits / baseUnits, 8, MidpointRounding.AwayFromZero),
            BaseCode = configuredBaseCode,
            SnapshotUtc = rawRate.SnapshotUtc
        };
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

    private static string ResolveConfiguredBaseCurrency(System.Data.IDbConnection connection)
    {
        var constants = connection.Query<AppConstantDto>(
            @"IF OBJECT_ID('dbo.AppConstants', 'U') IS NULL
                  SELECT CAST(NULL AS NVARCHAR(120)) AS [Key], CAST(NULL AS NVARCHAR(4000)) AS [Value]
                  WHERE 1 = 0;
              ELSE
                  SELECT [Key], [Value]
                  FROM dbo.AppConstants;")
            .ToDictionary(item => item.Key, item => item.Value, StringComparer.OrdinalIgnoreCase);

        return ResolveCurrencyCode(constants, "BaseCurrencyCode")
            ?? ResolveCurrencyCode(constants, "DefaultCurrencyCode")
            ?? "USD";
    }

    private static string? ResolveCurrencyCode(IReadOnlyDictionary<string, string> constants, string key)
    {
        if (!constants.TryGetValue(key, out var value))
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
