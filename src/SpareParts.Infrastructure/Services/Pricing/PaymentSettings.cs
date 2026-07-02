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
