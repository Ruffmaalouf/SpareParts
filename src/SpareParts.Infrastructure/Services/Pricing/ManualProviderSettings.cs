namespace SpareParts.Infrastructure.Services.Pricing;

public sealed class ManualProviderSettings
{
    public string BankName { get; init; } = string.Empty;
    public string AccountName { get; init; } = string.Empty;
    public string AccountNumber { get; init; } = string.Empty;
    public string Iban { get; init; } = string.Empty;
    public string OmtWhishNumber { get; init; } = string.Empty;
    public string Instructions { get; init; } = string.Empty;
}
