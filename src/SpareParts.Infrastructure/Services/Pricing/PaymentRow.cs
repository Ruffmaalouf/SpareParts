namespace SpareParts.Infrastructure.Services.Pricing;

/// <summary>Raw <c>dbo.Payments</c> row used by <see cref="PaymentService"/>.</summary>
internal sealed class PaymentRow
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public int? SubscriptionId { get; set; }
    public string ProviderCode { get; set; } = string.Empty;
    public string? ProviderPaymentId { get; set; }
    public string? ProviderCheckoutSessionId { get; set; }
    public string PackageCode { get; set; } = string.Empty;
    public int BillingCycle { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public int Status { get; set; }
    public string? Notes { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
