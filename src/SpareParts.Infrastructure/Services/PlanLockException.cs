namespace SpareParts.Infrastructure.Services
{
    /// <summary>
    /// Thrown when a tenant's pricing plan does not allow an action — either a numeric limit
    /// has been reached (PLAN_LIMIT_EXCEEDED) or a feature is not included in the plan (FEATURE_LOCKED).
    /// Mapped by ApiExceptionMiddleware to a 403 response carrying errorCode/currentPlan/requiredPlans/upgradeUrl.
    /// </summary>
    public sealed class PlanLockException : DomainException
    {
        public string ErrorCode { get; }
        public string CurrentPlan { get; }
        public IReadOnlyList<string> RequiredPlans { get; }
        public string UpgradeUrl { get; }

        public PlanLockException(string message, string errorCode, string currentPlan, IReadOnlyList<string> requiredPlans, string upgradeUrl)
            : base(message)
        {
            ErrorCode = errorCode;
            CurrentPlan = currentPlan;
            RequiredPlans = requiredPlans;
            UpgradeUrl = upgradeUrl;
        }
    }
}
