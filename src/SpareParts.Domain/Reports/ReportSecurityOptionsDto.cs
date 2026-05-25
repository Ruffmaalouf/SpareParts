namespace SpareParts.Domain.Reports;

public sealed class ReportSecurityOptionsDto
{
    public int CurrentRoleId { get; set; }
    public string DefaultBaseCurrencyCode { get; set; } = "USD";
    public string DefaultCounterCurrencyCode { get; set; } = "USD";
    public List<RoleSecurityOptionDto> Roles { get; set; } = new();
    public List<string> CurrencyCodes { get; set; } = new();
}

public sealed class RoleSecurityOptionDto
{
    public int RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
}
