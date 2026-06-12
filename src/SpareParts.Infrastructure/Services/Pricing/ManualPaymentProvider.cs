using SpareParts.Domain.Pricing;
using SpareParts.Infrastructure.Interfaces;

namespace SpareParts.Infrastructure.Services.Pricing;

/// <summary>Bank transfer / OMT / Whish payment provider. No external calls — returns instructions for the tenant
/// and leaves the payment Pending until an admin marks it as paid.</summary>
public sealed class ManualPaymentProvider : IPaymentProvider
{
    private readonly PaymentSettings _settings;

    public ManualPaymentProvider(PaymentSettings settings)
    {
        _settings = settings;
    }

    public string ProviderCode => PaymentProviderCode.Manual;

    public Task<CheckoutResponseDto> CreateCheckoutAsync(PaymentCheckoutContext context, CancellationToken cancellationToken)
    {
        return Task.FromResult(new CheckoutResponseDto
        {
            PaymentId = context.PaymentId,
            ProviderCode = ProviderCode,
            Status = "manual",
            Instructions = BuildInstructions(_settings.Providers.Manual, context)
        });
    }

    public Task<WebhookResult> HandleWebhookAsync(WebhookRequestContext context, CancellationToken cancellationToken)
    {
        return Task.FromResult(new WebhookResult
        {
            Success = false,
            Message = "The manual payment provider does not support webhooks. An admin must mark the payment as paid."
        });
    }

    private static string BuildInstructions(ManualProviderSettings manual, PaymentCheckoutContext context)
    {
        var lines = new List<string>
        {
            $"Please transfer {context.Amount:N2} {context.Currency} for your {context.PackageCode} ({context.BillingCycle}) subscription.",
            $"Reference: PAY-{context.PaymentId}"
        };

        if (!string.IsNullOrWhiteSpace(manual.BankName)) lines.Add($"Bank: {manual.BankName}");
        if (!string.IsNullOrWhiteSpace(manual.AccountName)) lines.Add($"Account name: {manual.AccountName}");
        if (!string.IsNullOrWhiteSpace(manual.AccountNumber)) lines.Add($"Account number: {manual.AccountNumber}");
        if (!string.IsNullOrWhiteSpace(manual.Iban)) lines.Add($"IBAN: {manual.Iban}");
        if (!string.IsNullOrWhiteSpace(manual.OmtWhishNumber)) lines.Add($"OMT/Whish number: {manual.OmtWhishNumber}");
        if (!string.IsNullOrWhiteSpace(manual.Instructions)) lines.Add(manual.Instructions);

        lines.Add("After paying, keep your reference number — an admin will activate your subscription once the payment is confirmed.");

        return string.Join("\n", lines);
    }
}
