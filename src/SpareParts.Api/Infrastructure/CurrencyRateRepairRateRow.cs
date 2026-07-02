namespace SpareParts.Api.Infrastructure;

/// <summary>Raw <c>dbo.CurrencyRates</c> row used by <see cref="AccountingCurrencyRateRepairMigration"/> to resolve currency conversion rates.</summary>
internal sealed class CurrencyRateRepairRateRow
{
    public string Code { get; set; } = string.Empty;
    public decimal RateToUsd { get; set; }
    public string BaseCode { get; set; } = string.Empty;
}
