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
                                 WHERE [Key] IN ('BaseCurrencyCode', 'DefaultCurrencyCode', 'CounterCurrencyCode', 'DefaultCounterRate');";

            var rows = session.Connection.Query<AppConstantRow>(sql, transaction: session.Transaction)
                .ToDictionary(item => item.Key, item => item.Value, StringComparer.OrdinalIgnoreCase);

            var baseCurrencyCode = ResolveCurrencyCode(rows, "BaseCurrencyCode")
                ?? ResolveCurrencyCode(rows, "DefaultCurrencyCode")
                ?? "USD";
            var counterCurrencyCode = ResolveCurrencyCode(rows, "CounterCurrencyCode")
                ?? baseCurrencyCode;
            var counterRateToBase = rows.TryGetValue("DefaultCounterRate", out var rawCounterRate)
                && decimal.TryParse(rawCounterRate, out var parsedCounterRate)
                && parsedCounterRate > 0m
                    ? decimal.Round(parsedCounterRate, 8, MidpointRounding.AwayFromZero)
                    : 1m;

            return new AccountingCurrencyContext
            {
                BaseCurrencyCode = baseCurrencyCode,
                CounterCurrencyCode = counterCurrencyCode,
                CounterRateToBase = counterRateToBase
            };
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
