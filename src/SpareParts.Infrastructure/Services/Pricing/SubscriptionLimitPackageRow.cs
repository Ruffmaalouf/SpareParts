namespace SpareParts.Infrastructure.Services.Pricing;

/// <summary>Minimal pricing-package fallback row (used when a tenant has no active subscription) for <see cref="SubscriptionLimitService"/>.</summary>
internal sealed class SubscriptionLimitPackageRow
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}
