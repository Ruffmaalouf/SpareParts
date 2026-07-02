namespace SpareParts.Infrastructure.Services;

/// <summary>Resolved sale/counter currency rates used by <see cref="UsedCarsService"/> when posting a used-car wholesale sale.</summary>
internal sealed record UsedCarWholesaleSaleCurrencyContext(
    decimal SaleRateToBase,
    string CounterCurrencyCode,
    decimal CounterRateToBase);
