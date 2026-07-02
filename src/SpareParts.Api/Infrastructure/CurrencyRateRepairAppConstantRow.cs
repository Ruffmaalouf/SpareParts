namespace SpareParts.Api.Infrastructure;

/// <summary>Raw <c>dbo.AppConstants</c> row used by <see cref="AccountingCurrencyRateRepairMigration"/> to resolve currency settings.</summary>
internal sealed class CurrencyRateRepairAppConstantRow
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}
