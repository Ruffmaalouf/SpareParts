namespace SpareParts.Api.Infrastructure;

/// <summary>Resolved base/counter currency codes and rate used by <see cref="AccountingCurrencyRateRepairMigration"/>.</summary>
internal sealed record CurrencyRepairContext(
    string BaseCurrencyCode,
    string CounterCurrencyCode,
    decimal CounterRateToBase);
