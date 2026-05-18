namespace SpareParts.Domain.Reports;

public sealed class ReportSecurityOptionsDto
{
    public string CurrentRoleName { get; set; } = string.Empty;
    public string DefaultBaseCurrencyCode { get; set; } = "USD";
    public string DefaultCounterCurrencyCode { get; set; } = "USD";
    public List<string> RoleNames { get; set; } = new();
    public List<string> CurrencyCodes { get; set; } = new();
}
