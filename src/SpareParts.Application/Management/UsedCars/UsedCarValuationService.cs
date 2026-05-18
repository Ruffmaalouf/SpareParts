using SpareParts.Application.Management.Currencies;

namespace SpareParts.Application.Management.UsedCars;

public sealed class UsedCarValuationService
{
    private readonly CurrencyRateProjectionService _currencyRateProjectionService;

    public UsedCarValuationService(CurrencyRateProjectionService currencyRateProjectionService)
    {
        _currencyRateProjectionService = currencyRateProjectionService;
    }

    public decimal ConvertAmountToCounterCurrency(
        CurrencyRateContext context,
        decimal amount,
        string? sourceCurrencyCode)
    {
        var normalizedAmount = Math.Max(amount, 0m);
        var rateToCounterCurrency = _currencyRateProjectionService.ResolveRateToCounterCurrency(context, sourceCurrencyCode);

        return rateToCounterCurrency > 0
            ? decimal.Round(normalizedAmount * rateToCounterCurrency, 2, MidpointRounding.AwayFromZero)
            : 0m;
    }

    public UsedCarValuationResult Calculate(CurrencyRateContext context, UsedCarValuationInput input)
    {
        var selectedToBaseRate = _currencyRateProjectionService.ResolveRateToBaseCurrency(context, input.PriceCurrencyCode);
        if (selectedToBaseRate <= 0)
        {
            return new UsedCarValuationResult();
        }

        var counterToBaseRate = _currencyRateProjectionService.ResolveRateToBaseCurrency(context, context.CounterCurrencyCode);
        if (counterToBaseRate <= 0)
        {
            counterToBaseRate = context.DefaultCounterRate > 0 ? context.DefaultCounterRate : 1m;
        }

        var normalizedPrice = Math.Max(input.Price, 0m);
        var convertedBasePrice = decimal.Round(normalizedPrice * selectedToBaseRate, 2, MidpointRounding.AwayFromZero);
        var convertedCounterPrice = counterToBaseRate > 0
            ? decimal.Round(convertedBasePrice / counterToBaseRate, 2, MidpointRounding.AwayFromZero)
            : convertedBasePrice;

        var counterExpensesTotal = Math.Max(input.Transportation, 0m)
            + Math.Max(input.PartOut, 0m)
            + Math.Max(input.Shipping, 0m)
            + Math.Max(input.Customs, 0m)
            + Math.Max(input.Repairs, 0m);

        return new UsedCarValuationResult
        {
            PriceBase = convertedBasePrice,
            PriceCounter = convertedCounterPrice,
            TotalBeforeShipping = decimal.Round(
                convertedCounterPrice + Math.Max(input.Transportation, 0m),
                2,
                MidpointRounding.AwayFromZero),
            GrandTotalCounter = decimal.Round(
                convertedCounterPrice + counterExpensesTotal,
                2,
                MidpointRounding.AwayFromZero),
            GrandTotalBase = decimal.Round(
                convertedBasePrice + (counterExpensesTotal * counterToBaseRate),
                2,
                MidpointRounding.AwayFromZero)
        };
    }
}
