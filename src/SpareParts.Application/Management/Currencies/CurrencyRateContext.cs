using SpareParts.Domain.MasterData;

namespace SpareParts.Application.Management.Currencies;

public sealed class CurrencyRateContext
{
    public string BaseCurrencyCode { get; init; } = "USD";
    public string CounterCurrencyCode { get; init; } = "USD";
    public decimal DefaultCounterRate { get; init; } = 1m;
    public IReadOnlyList<CurrencyRateDto> Rates { get; init; } = Array.Empty<CurrencyRateDto>();
}
