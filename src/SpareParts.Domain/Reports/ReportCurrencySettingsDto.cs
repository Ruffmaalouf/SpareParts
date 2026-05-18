namespace SpareParts.Domain.Reports;

public sealed class ReportCurrencySettingsDto
{
    public bool IsEnabled { get; set; }
    public string? AmountColumnKey { get; set; }
    public string? CurrencyCodeColumnKey { get; set; }
    public string? RateToBaseColumnKey { get; set; }
    public string? BaseCurrencyCodeColumnKey { get; set; }
    public string? CounterCurrencyCodeColumnKey { get; set; }
    public string? CounterRateToBaseColumnKey { get; set; }
    public string ReportingCurrencyCode { get; set; } = "USD";
}
