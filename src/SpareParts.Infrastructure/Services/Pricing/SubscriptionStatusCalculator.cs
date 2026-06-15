using SpareParts.Domain.Pricing;

namespace SpareParts.Infrastructure.Services.Pricing;

/// <summary>
/// Computes the time-based effective status of a subscription. The persisted Status column is
/// updated periodically by <c>ISubscriptionService.RunMaintenance()</c>, but read paths also
/// apply this calculation so API responses are accurate even between maintenance runs.
/// </summary>
internal static class SubscriptionStatusCalculator
{
    /// <summary>How long a subscription stays "PastDue" after its period ends before being marked "Expired".</summary>
    public const int PastDueGracePeriodDays = 7;

    public static SubscriptionStatus ResolveEffectiveStatus(SubscriptionStatus status, DateTime currentPeriodEnd, DateTime? trialEndsAt, DateTime now)
    {
        return status switch
        {
            SubscriptionStatus.Trialing when trialEndsAt is { } trialEnd && trialEnd < now => SubscriptionStatus.Expired,
            SubscriptionStatus.Active when currentPeriodEnd < now => SubscriptionStatus.PastDue,
            SubscriptionStatus.PastDue when currentPeriodEnd.AddDays(PastDueGracePeriodDays) < now => SubscriptionStatus.Expired,
            _ => status
        };
    }

    /// <summary>
    /// True when the effective status still grants access to the subscribed package's features and limits.
    /// Matches <c>ISubscriptionLimitService</c>'s fallback rules: PastDue, Cancelled and Expired all fall
    /// back to the FREE plan for enforcement purposes.
    /// </summary>
    public static bool GrantsPlanAccess(SubscriptionStatus effectiveStatus) =>
        effectiveStatus is SubscriptionStatus.Trialing or SubscriptionStatus.Active;
}
