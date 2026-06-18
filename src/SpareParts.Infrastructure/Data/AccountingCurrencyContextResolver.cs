using Dapper;

namespace SpareParts.Infrastructure.Data
{
    internal static class AccountingCurrencyContextResolver
    {
        public static AccountingCurrencyContext Resolve(DbSession session)
        {
            if (!AccountingSchemaInspector.HasTable(session, "dbo.AppConstants"))
            {
                return new AccountingCurrencyContext();
            }

            const string sql = @"SELECT [Key], [Value]
                                 FROM dbo.AppConstants
                                 WHERE [Key] IN ('BaseCurrencyCode', 'DefaultCurrencyCode', 'CounterCurrencyCode', 'DisplayCurrencyCode', 'DefaultCounterRate')
                                   AND (@TenantId = 0 OR TenantId = @TenantId);";

            var rows = session.Connection.Query<AppConstantRow>(sql, new { session.TenantId }, transaction: session.Transaction)
                .ToDictionary(item => item.Key, item => item.Value, StringComparer.OrdinalIgnoreCase);

            var baseCurrencyCode = ResolveCurrencyCode(rows, "BaseCurrencyCode")
                ?? ResolveCurrencyCode(rows, "DefaultCurrencyCode")
                ?? "USD";
            var counterCurrencyCode = ResolveCurrencyCode(rows, "CounterCurrencyCode")
                ?? baseCurrencyCode;
            var displayCurrencyCode = ResolveCurrencyCode(rows, "DisplayCurrencyCode")
                ?? counterCurrencyCode;
            var configuredCounterRateToBase = rows.TryGetValue("DefaultCounterRate", out var rawCounterRate)
                && decimal.TryParse(rawCounterRate, out var parsedCounterRate)
                && parsedCounterRate > 0m
                    ? decimal.Round(parsedCounterRate, 8, MidpointRounding.AwayFromZero)
                    : 1m;

            var ratesByCode = LoadCurrencyRates(session);
            var counterRateToBase = ResolveRateToBaseCurrency(ratesByCode, baseCurrencyCode, counterCurrencyCode)
                ?? configuredCounterRateToBase;
            var displayRateToBase = ResolveRateToBaseCurrency(ratesByCode, baseCurrencyCode, displayCurrencyCode)
                ?? ResolveDisplayRateToBase(
                displayCurrencyCode,
                baseCurrencyCode,
                counterCurrencyCode,
                counterRateToBase);

            return new AccountingCurrencyContext
            {
                BaseCurrencyCode = baseCurrencyCode,
                CounterCurrencyCode = counterCurrencyCode,
                DisplayCurrencyCode = displayCurrencyCode,
                CounterRateToBase = counterRateToBase,
                DisplayRateToBase = displayRateToBase
            };
        }

        public static decimal ResolveRateToBaseCurrency(
            DbSession session,
            string baseCurrencyCode,
            string currencyCode,
            string? counterCurrencyCode = null,
            decimal defaultCounterRate = 1m)
        {
            var ratesByCode = LoadCurrencyRates(session);
            var resolvedRate = ResolveRateToBaseCurrency(ratesByCode, baseCurrencyCode, currencyCode);
            if (resolvedRate is > 0m)
            {
                return resolvedRate.Value;
            }

            var normalizedCurrencyCode = NormalizeCurrencyCode(currencyCode);
            var normalizedCounterCode = NormalizeCurrencyCode(counterCurrencyCode);
            if (normalizedCurrencyCode is not null
                && normalizedCounterCode is not null
                && string.Equals(normalizedCurrencyCode, normalizedCounterCode, StringComparison.OrdinalIgnoreCase)
                && defaultCounterRate > 0m)
            {
                return decimal.Round(defaultCounterRate, 8, MidpointRounding.AwayFromZero);
            }

            return 0m;
        }

        private static IReadOnlyDictionary<string, CurrencyRateRow> LoadCurrencyRates(DbSession session)
        {
            if (!AccountingSchemaInspector.HasTable(session, "dbo.CurrencyRates"))
            {
                return new Dictionary<string, CurrencyRateRow>(StringComparer.OrdinalIgnoreCase);
            }

            const string sql = @"SELECT Code, RateToUsd, BaseCode
                                 FROM dbo.CurrencyRates
                                 WHERE (@TenantId = 0 OR TenantId = @TenantId);";

            return session.Connection.Query<CurrencyRateRow>(sql, new { session.TenantId }, transaction: session.Transaction)
                .Select(rate => new { Rate = rate, Code = NormalizeCurrencyCode(rate.Code) })
                .Where(item => item.Code is not null && item.Rate.RateToUsd > 0m)
                .GroupBy(item => item.Code!, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.Last().Rate, StringComparer.OrdinalIgnoreCase);
        }

        private static decimal? ResolveRateToBaseCurrency(
            IReadOnlyDictionary<string, CurrencyRateRow> ratesByCode,
            string baseCurrencyCode,
            string currencyCode)
        {
            var normalizedBaseCode = NormalizeCurrencyCode(baseCurrencyCode) ?? "USD";
            var normalizedCurrencyCode = NormalizeCurrencyCode(currencyCode);
            if (normalizedCurrencyCode == null)
            {
                return null;
            }

            if (string.Equals(normalizedCurrencyCode, normalizedBaseCode, StringComparison.OrdinalIgnoreCase))
            {
                return 1m;
            }

            if (ratesByCode.Count == 0)
            {
                return null;
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
            IReadOnlyDictionary<string, CurrencyRateRow> ratesByCode,
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

            var rateBaseCode = NormalizeCurrencyCode(rate.BaseCode) ?? currencyCode;
            decimal resolvedUnits;
            if (string.Equals(currencyCode, rateBaseCode, StringComparison.OrdinalIgnoreCase))
            {
                resolvedUnits = 1m;
            }
            else
            {
                var baseUnits = ResolveUnitsPerReferenceCurrency(rateBaseCode, ratesByCode, unitsPerReferenceCurrency, activeStack);
                resolvedUnits = baseUnits > 0m && rate.RateToUsd > 0m
                    ? decimal.Round(rate.RateToUsd * baseUnits, 12, MidpointRounding.AwayFromZero)
                    : 0m;
            }

            activeStack.Remove(currencyCode);
            unitsPerReferenceCurrency[currencyCode] = resolvedUnits;
            return resolvedUnits;
        }

        private static decimal ResolveDisplayRateToBase(
            string displayCurrencyCode,
            string baseCurrencyCode,
            string counterCurrencyCode,
            decimal counterRateToBase)
        {
            if (string.Equals(displayCurrencyCode, baseCurrencyCode, StringComparison.OrdinalIgnoreCase))
            {
                return 1m;
            }

            if (string.Equals(displayCurrencyCode, counterCurrencyCode, StringComparison.OrdinalIgnoreCase))
            {
                return counterRateToBase > 0m ? counterRateToBase : 1m;
            }

            return 1m;
        }

        private static string? ResolveCurrencyCode(IReadOnlyDictionary<string, string> values, string key)
        {
            if (!values.TryGetValue(key, out var rawValue))
            {
                return null;
            }

            return NormalizeCurrencyCode(rawValue);
        }

        private static string? NormalizeCurrencyCode(string? rawValue)
        {
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return null;
            }

            var normalized = rawValue.Trim().ToUpperInvariant();
            return normalized.Length == 3 ? normalized : null;
        }

    }
}
