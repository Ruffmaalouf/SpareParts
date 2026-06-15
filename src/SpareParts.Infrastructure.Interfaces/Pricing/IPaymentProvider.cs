using SpareParts.Domain.Pricing;

namespace SpareParts.Infrastructure.Interfaces
{
    public sealed class PaymentCheckoutContext
    {
        public int PaymentId { get; set; }
        public int TenantId { get; set; }
        public string PackageCode { get; set; } = string.Empty;
        public string BillingCycle { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "USD";
        public string SuccessUrl { get; set; } = string.Empty;
        public string CancelUrl { get; set; } = string.Empty;
    }

    public sealed class WebhookRequestContext
    {
        public string RawBody { get; set; } = string.Empty;
        public IReadOnlyDictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();
        public IReadOnlyDictionary<string, string> Query { get; set; } = new Dictionary<string, string>();
    }

    public sealed class WebhookResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? ProviderEventId { get; set; }
        public string? ProviderPaymentId { get; set; }
        public string? EventType { get; set; }

        /// <summary>Null when the event does not correspond to a known payment.</summary>
        public bool? PaymentSucceeded { get; set; }
    }

    /// <summary>A pluggable payment provider (Manual bank transfer, Test, Stripe, ...).</summary>
    public interface IPaymentProvider
    {
        string ProviderCode { get; }

        Task<CheckoutResponseDto> CreateCheckoutAsync(PaymentCheckoutContext context, CancellationToken cancellationToken);

        Task<WebhookResult> HandleWebhookAsync(WebhookRequestContext context, CancellationToken cancellationToken);
    }

    public interface IPaymentProviderFactory
    {
        IPaymentProvider GetProvider(string providerCode);

        IPaymentProvider GetDefaultProvider();
    }
}
