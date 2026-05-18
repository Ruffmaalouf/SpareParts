namespace SpareParts.Infrastructure.Services;

internal sealed class ReportCurrencyProjectionContext
{
    public bool IsEnabled { get; set; }
    public string TransactionCurrencyExpression { get; set; } = string.Empty;
    public string BaseCurrencyCodeExpression { get; set; } = string.Empty;
    public string CounterCurrencyCodeExpression { get; set; } = string.Empty;
    public string BaseAmountExpression { get; set; } = string.Empty;
    public string CounterAmountExpression { get; set; } = string.Empty;
    public string ReportingAmountExpression { get; set; } = string.Empty;
    public string ReportingCurrencyCode { get; set; } = "USD";
}
