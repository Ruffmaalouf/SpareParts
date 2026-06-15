namespace SpareParts.Domain.Pricing;

public sealed class UpsertPricingPackageRequest
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
    public List<PackageFeatureDto> Features { get; set; } = [];
    public List<PackageLimitDto> Limits { get; set; } = [];
}

public sealed class SetPackageActiveRequest
{
    public bool IsActive { get; set; }
}

public sealed class StartTrialRequest
{
    public string PackageCode { get; set; } = global::SpareParts.Domain.Pricing.PackageCode.Pro;
    public int TrialDays { get; set; } = 14;
}

public sealed class CheckoutRequest
{
    public string PackageCode { get; set; } = string.Empty;

    /// <summary>"Monthly" or "Yearly".</summary>
    public string BillingCycle { get; set; } = "Monthly";

    /// <summary>Optional override of the configured default payment provider.</summary>
    public string? ProviderCode { get; set; }
}

public sealed class ChangePlanRequest
{
    public string PackageCode { get; set; } = string.Empty;

    /// <summary>"Monthly" or "Yearly". Keeps current cycle when omitted.</summary>
    public string? BillingCycle { get; set; }
}

public sealed class CancelSubscriptionRequest
{
    /// <summary>When true, cancels immediately (downgrades to FREE now). Otherwise cancels at period end.</summary>
    public bool Immediate { get; set; }
}

public sealed class AdminActivateSubscriptionRequest
{
    public int TenantId { get; set; }
    public string PackageCode { get; set; } = string.Empty;

    /// <summary>"Monthly" or "Yearly".</summary>
    public string BillingCycle { get; set; } = "Monthly";
    public int? PaymentId { get; set; }
    public int? DurationDays { get; set; }
}

public sealed class MarkPaymentPaidRequest
{
    public string? Notes { get; set; }
    public string? ProviderPaymentId { get; set; }
}

public sealed class UpdatePartImagesRequest
{
    public List<string> ImageUrls { get; set; } = [];
}
