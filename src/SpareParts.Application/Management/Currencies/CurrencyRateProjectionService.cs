namespace SpareParts.Application.Management.Currencies;

public sealed class CurrencyRateProjectionService
{
    public string? NormalizeCurrencyCode(string? currencyCode)
    {
        if (string.IsNullOrWhiteSpace(currencyCode))
        {
            return null;
        }

        var normalized = currencyCode.Trim().ToUpperInvariant();
        return normalized.Length == 3 ? normalized : null;
    }

    public IReadOnlyList<string> BuildCurrencyCodes(CurrencyRateContext context)
    {
        var codes = context.Rates
            .Select(rate => NormalizeCurrencyCode(rate.Code))
            .OfType<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(code => code, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (!codes.Contains(context.BaseCurrencyCode, StringComparer.OrdinalIgnoreCase))
        {
            codes.Insert(0, context.BaseCurrencyCode);
        }

        if (!codes.Contains(context.CounterCurrencyCode, StringComparer.OrdinalIgnoreCase))
        {
            codes.Add(context.CounterCurrencyCode);
        }

        return codes;
    }

    public IReadOnlyList<CurrencyRateProjection> BuildProjections(CurrencyRateContext context)
    {
        return BuildCurrencyCodes(context)
            .Select(code => new CurrencyRateProjection
            {
                Code = code,
                BaseRate = decimal.Round(ResolveRateToBaseCurrency(context, code), 6, MidpointRounding.AwayFromZero),
                CounterRate = decimal.Round(ResolveRateToCounterCurrency(context, code), 6, MidpointRounding.AwayFromZero),
                SnapshotUtc = context.Rates.FirstOrDefault(rate =>
                    string.Equals(NormalizeCurrencyCode(rate.Code), code, StringComparison.OrdinalIgnoreCase))?.SnapshotUtc
            })
            .ToList();
    }

    public decimal ResolveRateToBaseCurrency(CurrencyRateContext context, string? currencyCode)
    {
        var normalizedCurrencyCode = NormalizeCurrencyCode(currencyCode);
        if (normalizedCurrencyCode == null)
        {
            return 0m;
        }

        if (string.Equals(normalizedCurrencyCode, context.BaseCurrencyCode, StringComparison.OrdinalIgnoreCase))
        {
            return 1m;
        }

        var currency = context.Rates.FirstOrDefault(rate =>
            string.Equals(NormalizeCurrencyCode(rate.Code), normalizedCurrencyCode, StringComparison.OrdinalIgnoreCase));
        if (currency == null || currency.RateToUsd <= 0)
        {
            return string.Equals(normalizedCurrencyCode, context.CounterCurrencyCode, StringComparison.OrdinalIgnoreCase)
                ? context.DefaultCounterRate
                : 0m;
        }

        var normalizedBaseCode = NormalizeCurrencyCode(currency.BaseCode) ?? context.BaseCurrencyCode;
        if (string.Equals(normalizedCurrencyCode, normalizedBaseCode, StringComparison.OrdinalIgnoreCase))
        {
            return 1m;
        }

        return 1m / currency.RateToUsd;
    }

    public decimal ResolveRateToCounterCurrency(CurrencyRateContext context, string? currencyCode)
    {
        var rateToBaseCurrency = ResolveRateToBaseCurrency(context, currencyCode);
        if (rateToBaseCurrency <= 0)
        {
            return 0m;
        }

        var counterRateToBaseCurrency = ResolveRateToBaseCurrency(context, context.CounterCurrencyCode);
        if (counterRateToBaseCurrency <= 0)
        {
            counterRateToBaseCurrency = context.DefaultCounterRate;
        }

        if (counterRateToBaseCurrency <= 0)
        {
            return 0m;
        }

        return rateToBaseCurrency / counterRateToBaseCurrency;
    }
}
