namespace SpareParts.Infrastructure.Interfaces
{
    /// <summary>
    /// Resolves the active pricing package for a tenant and enforces its feature flags
    /// and numeric limits. Throws SpareParts.Infrastructure.Services.PlanLockException
    /// (mapped by the API to a 403 PLAN_LIMIT_EXCEEDED / FEATURE_LOCKED response) when blocked.
    /// </summary>
    public interface ISubscriptionLimitService
    {
        /// <summary>The package code currently active for the tenant (falls back to FREE).</summary>
        string GetActivePackageCode(int tenantId);

        bool HasFeature(int tenantId, string featureCode);

        /// <summary>Throws PlanLockException(FEATURE_LOCKED) when the feature is not enabled for the tenant's plan.</summary>
        void EnsureFeatureEnabled(int tenantId, string featureCode, string featureDisplayName);

        /// <summary>Returns the numeric limit for the tenant's active plan. -1 means unlimited.</summary>
        int GetLimitValue(int tenantId, string limitCode);

        /// <summary>
        /// Throws PlanLockException(PLAN_LIMIT_EXCEEDED) when currentCount + additional would exceed
        /// the tenant's plan limit for limitCode (no-op when the limit is unlimited).
        /// </summary>
        void EnsureWithinLimit(int tenantId, string limitCode, int currentCount, string limitDisplayName, int additional = 1);

        /// <summary>Returns the current calendar-month usage value for a monthly counter (0 if none recorded).</summary>
        int GetMonthlyUsage(int tenantId, string counterCode);

        /// <summary>
        /// Throws PlanLockException(PLAN_LIMIT_EXCEEDED) when this month's usage + additional would exceed
        /// the tenant's plan limit for counterCode; otherwise increments the counter and returns the new value.
        /// </summary>
        int EnsureAndIncrementMonthlyUsage(int tenantId, string counterCode, string limitDisplayName, int additional = 1);
    }
}
