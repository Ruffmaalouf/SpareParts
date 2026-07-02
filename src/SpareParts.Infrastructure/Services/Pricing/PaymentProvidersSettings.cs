namespace SpareParts.Infrastructure.Services.Pricing;

public sealed class PaymentProvidersSettings
{
    public TestProviderSettings Test { get; init; } = new();
    public ManualProviderSettings Manual { get; init; } = new();
    public StripeProviderSettings Stripe { get; init; } = new();
}
