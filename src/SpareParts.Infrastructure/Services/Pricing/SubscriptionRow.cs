namespace SpareParts.Infrastructure.Services.Pricing;

/// <summary>Raw tenant subscription + package projection used by <see cref="SubscriptionService"/>.</summary>
internal sealed class SubscriptionRow
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public string TenantName { get; set; } = string.Empty;
    public string TenantCode { get; set; } = string.Empty;
    public int PackageId { get; set; }
    public string PackageCode { get; set; } = string.Empty;
    public string PackageName { get; set; } = string.Empty;
    public int PackageSortOrder { get; set; }
    public int Status { get; set; }
    public int BillingCycle { get; set; }
    public DateTime CurrentPeriodStart { get; set; }
    public DateTime CurrentPeriodEnd { get; set; }
    public DateTime? TrialEndsAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public bool CancelAtPeriodEnd { get; set; }
    public string? ProviderCode { get; set; }
    public string? ProviderSubscriptionId { get; set; }
}
