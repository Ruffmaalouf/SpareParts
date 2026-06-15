using SpareParts.Domain.Common;

namespace SpareParts.Domain.Pricing;

public sealed class PricingPackage : AuditableEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal MonthlyPrice { get; set; }
    public decimal YearlyPrice { get; set; }
    public string Currency { get; set; } = "USD";
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsCustomPricing { get; set; }
}

public sealed class PackageFeature : AuditableEntity
{
    public int PackageId { get; set; }
    public string FeatureCode { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
}

public sealed class PackageLimit : AuditableEntity
{
    public int PackageId { get; set; }
    public string LimitCode { get; set; } = string.Empty;

    /// <summary>-1 means unlimited.</summary>
    public int LimitValue { get; set; }
}

public sealed class TenantSubscription : AuditableEntity
{
    public int TenantId { get; set; }
    public int PackageId { get; set; }
    public SubscriptionStatus Status { get; set; }
    public BillingCycle BillingCycle { get; set; }
    public DateTime CurrentPeriodStart { get; set; }
    public DateTime CurrentPeriodEnd { get; set; }
    public DateTime? TrialEndsAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public bool CancelAtPeriodEnd { get; set; }
    public string? ProviderCode { get; set; }
    public string? ProviderSubscriptionId { get; set; }
}

public sealed class TenantUsageCounter : AuditableEntity
{
    public int TenantId { get; set; }
    public string CounterCode { get; set; } = string.Empty;
    public int PeriodYear { get; set; }
    public int PeriodMonth { get; set; }
    public int CurrentValue { get; set; }
}

public sealed class Payment : AuditableEntity
{
    public int TenantId { get; set; }
    public int? SubscriptionId { get; set; }
    public string ProviderCode { get; set; } = string.Empty;
    public string? ProviderPaymentId { get; set; }
    public string? ProviderCheckoutSessionId { get; set; }
    public string PackageCode { get; set; } = string.Empty;
    public BillingCycle BillingCycle { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public PaymentStatus Status { get; set; }
    public string? Notes { get; set; }
    public DateTime? PaidAt { get; set; }
}

public sealed class Invoice : AuditableEntity
{
    public int TenantId { get; set; }
    public int? SubscriptionId { get; set; }
    public int? PaymentId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public decimal Subtotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "USD";
    public InvoiceStatus Status { get; set; }
    public DateTime IssuedAt { get; set; }
    public DateTime? DueAt { get; set; }
    public DateTime? PaidAt { get; set; }
}

public sealed class PaymentTransaction : AuditableEntity
{
    public int PaymentId { get; set; }
    public string ProviderCode { get; set; } = string.Empty;
    public PaymentTransactionStatus Status { get; set; }
    public string? RawPayload { get; set; }
    public string? Message { get; set; }
}

public sealed class WebhookEvent : AuditableEntity
{
    public string Provider { get; set; } = string.Empty;
    public string EventId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public WebhookProcessingStatus Status { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string? Error { get; set; }
}
