namespace SpareParts.Infrastructure.Services.Pricing;

/// <summary>Manual-testing webhook payload shape accepted by <see cref="TestPaymentProvider"/>: <c>{ "paymentId": 1, "succeeded": true }</c>.</summary>
internal sealed class TestWebhookPayload
{
    public int PaymentId { get; set; }
    public bool Succeeded { get; set; } = true;
    public string? EventId { get; set; }
}
