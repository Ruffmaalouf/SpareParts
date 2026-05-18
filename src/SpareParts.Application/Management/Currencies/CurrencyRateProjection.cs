namespace SpareParts.Application.Management.Currencies;

public sealed class CurrencyRateProjection
{
    public string Code { get; init; } = string.Empty;
    public decimal BaseRate { get; init; }
    public decimal CounterRate { get; init; }
    public DateTime? SnapshotUtc { get; init; }
}
