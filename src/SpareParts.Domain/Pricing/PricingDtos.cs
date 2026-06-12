namespace SpareParts.Domain.Pricing;

public sealed class PackageFeatureDto
{
    public string FeatureCode { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
}

public sealed class PackageLimitDto
{
    public string LimitCode { get; set; } = string.Empty;

    /// <summary>-1 means unlimited.</summary>
    public int LimitValue { get; set; }
}

public sealed class PricingPackageDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal MonthlyPrice { get; set; }
    public decimal YearlyPrice { get; set; }
    public string Currency { get; set; } = "USD";
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
    public bool IsCustomPricing { get; set; }
    public IReadOnlyList<PackageFeatureDto> Features { get; set; } = [];
    public IReadOnlyList<PackageLimitDto> Limits { get; set; } = [];
}

public sealed class UsageCounterDto
{
    public string CounterCode { get; set; } = string.Empty;
    public int CurrentValue { get; set; }

    /// <summary>-1 means unlimited.</summary>
    public int LimitValue { get; set; }
}

public sealed class TenantSubscriptionDto
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public string TenantName { get; set; } = string.Empty;
    public string TenantCode { get; set; } = string.Empty;
    public int PackageId { get; set; }
    public string PackageCode { get; set; } = string.Empty;
    public string PackageName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string BillingCycle { get; set; } = string.Empty;
    public DateTime CurrentPeriodStart { get; set; }
    public DateTime CurrentPeriodEnd { get; set; }
    public DateTime? TrialEndsAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public bool CancelAtPeriodEnd { get; set; }
    public IReadOnlyList<PackageFeatureDto> Features { get; set; } = [];
    public IReadOnlyList<PackageLimitDto> Limits { get; set; } = [];
    public IReadOnlyList<UsageCounterDto> Usage { get; set; } = [];
}

public sealed class PaymentDto
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public int? SubscriptionId { get; set; }
    public string ProviderCode { get; set; } = string.Empty;
    public string? ProviderPaymentId { get; set; }
    public string? ProviderCheckoutSessionId { get; set; }
    public string PackageCode { get; set; } = string.Empty;
    public string BillingCycle { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class InvoiceDto
{
    public int Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public int TenantId { get; set; }
    public int? SubscriptionId { get; set; }
    public int? PaymentId { get; set; }
    public decimal Subtotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "USD";
    public string Status { get; set; } = string.Empty;
    public DateTime IssuedAt { get; set; }
    public DateTime? DueAt { get; set; }
    public DateTime? PaidAt { get; set; }
}

public sealed class WebhookEventDto
{
    public int Id { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string EventId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? ProcessedAt { get; set; }
    public string? Error { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class CheckoutResponseDto
{
    public int PaymentId { get; set; }
    public string ProviderCode { get; set; } = string.Empty;

    /// <summary>"redirect" | "manual" | "succeeded".</summary>
    public string Status { get; set; } = string.Empty;
    public string? RedirectUrl { get; set; }
    public string? CheckoutSessionId { get; set; }
    public string? Instructions { get; set; }
}

/// <summary>
/// Structured payload for blocked requests. Returned by the API as the body of a 403 response
/// when a tenant's plan does not allow an action (over a numeric limit, or a locked feature).
/// </summary>
public sealed class PlanLockDto
{
    /// <summary>PLAN_LIMIT_EXCEEDED or FEATURE_LOCKED.</summary>
    public string ErrorCode { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string CurrentPlan { get; set; } = string.Empty;
    public IReadOnlyList<string> RequiredPlans { get; set; } = [];
    public string UpgradeUrl { get; set; } = string.Empty;
}
