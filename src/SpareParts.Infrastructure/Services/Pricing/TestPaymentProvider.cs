using System.Text.Json;
using SpareParts.Domain.Pricing;
using SpareParts.Infrastructure.Interfaces;

namespace SpareParts.Infrastructure.Services.Pricing;

/// <summary>Development/QA payment provider. Checkout completes immediately (success or, when
/// <see cref="TestProviderSettings.ForceFailure"/> is set, failure) and webhooks accept a simple
/// JSON body of the form <c>{ "paymentId": 1, "succeeded": true }</c> for manual testing.</summary>
public sealed class TestPaymentProvider : IPaymentProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly PaymentSettings _settings;

    public TestPaymentProvider(PaymentSettings settings)
    {
        _settings = settings;
    }

    public string ProviderCode => PaymentProviderCode.Test;

    public Task<CheckoutResponseDto> CreateCheckoutAsync(PaymentCheckoutContext context, CancellationToken cancellationToken)
    {
        var forceFailure = _settings.Providers.Test.ForceFailure;

        return Task.FromResult(new CheckoutResponseDto
        {
            PaymentId = context.PaymentId,
            ProviderCode = ProviderCode,
            Status = forceFailure ? "failed" : "succeeded",
            RedirectUrl = forceFailure ? context.CancelUrl : context.SuccessUrl
        });
    }

    public Task<WebhookResult> HandleWebhookAsync(WebhookRequestContext context, CancellationToken cancellationToken)
    {
        TestWebhookPayload? payload;
        try
        {
            payload = JsonSerializer.Deserialize<TestWebhookPayload>(context.RawBody, JsonOptions);
        }
        catch (JsonException)
        {
            return Task.FromResult(new WebhookResult { Success = false, Message = "Invalid JSON payload." });
        }

        if (payload is null || payload.PaymentId <= 0)
        {
            return Task.FromResult(new WebhookResult { Success = false, Message = "Missing 'paymentId' in webhook payload." });
        }

        return Task.FromResult(new WebhookResult
        {
            Success = true,
            Message = "Processed test webhook event.",
            ProviderEventId = string.IsNullOrWhiteSpace(payload.EventId)
                ? $"test-{payload.PaymentId}-{Guid.NewGuid():N}"
                : payload.EventId,
            ProviderPaymentId = payload.PaymentId.ToString(),
            EventType = payload.Succeeded ? "payment.succeeded" : "payment.failed",
            PaymentSucceeded = payload.Succeeded
        });
    }

    private sealed class TestWebhookPayload
    {
        public int PaymentId { get; set; }
        public bool Succeeded { get; set; } = true;
        public string? EventId { get; set; }
    }
}
