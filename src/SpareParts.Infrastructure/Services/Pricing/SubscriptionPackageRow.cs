namespace SpareParts.Infrastructure.Services.Pricing;

/// <summary>Effective subscription + package projection used by <see cref="SubscriptionLimitService"/> to resolve plan limits/features.</summary>
internal sealed class SubscriptionPackageRow
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public int Status { get; set; }
    public DateTime? TrialEndsAt { get; set; }
    public DateTime CurrentPeriodEnd { get; set; }
}
