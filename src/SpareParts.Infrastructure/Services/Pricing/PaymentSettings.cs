using SpareParts.Domain.Pricing;

namespace SpareParts.Infrastructure.Services.Pricing;

public sealed class PaymentSettings
{
    public string DefaultProvider { get; init; } = PaymentProviderCode.Manual;
    public string Currency { get; init; } = "USD";
    public string SuccessUrl { get; init; } = "/billing/payment-success";
    public string CancelUrl { get; init; } = "/billing/payment-failed";
    public string WebhookBaseUrl { get; init; } = string.Empty;
    public bool TaxEnabled { get; init; }
    public decimal TaxPercentage { get; init; }
    public string ManualPaymentInstructions { get; init; } = string.Empty;
    public PaymentProvidersSettings Providers { get; init; } = new();
}

public sealed class PaymentProvidersSettings
{
    public TestProviderSettings Test { get; init; } = new();
    public ManualProviderSettings Manual { get; init; } = new();
    public StripeProviderSettings Stripe { get; init; } = new();
}

public sealed class TestProviderSettings
{
    /// <summary>When true, the test provider's checkout always reports a failed payment. Useful for QA.</summary>
    public bool ForceFailure { get; init; }
}

public sealed class ManualProviderSettings
{
    public string BankName { get; init; } = string.Empty;
    public string AccountName { get; init; } = string.Empty;
    public string AccountNumber { get; init; } = string.Empty;
    public string Iban { get; init; } = string.Empty;
    public string OmtWhishNumber { get; init; } = string.Empty;
    public string Instructions { get; init; } = string.Empty;
}

public sealed class StripeProviderSettings
{
    public bool Enabled { get; init; }
    public string PublishableKey { get; init; } = string.Empty;

    /// <summary>Resolved from environment/user-secrets only — never stored in appsettings.json.</summary>
    public string SecretKey { get; init; } = string.Empty;
    public string WebhookSecret { get; init; } = string.Empty;
}
