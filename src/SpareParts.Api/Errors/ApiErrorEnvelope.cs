namespace SpareParts.Api.Errors
{
    public sealed class ApiErrorEnvelope
    {
        public string Code { get; init; } = string.Empty;
        public string Message { get; init; } = string.Empty;
        public string? TraceId { get; init; }

        /// <summary>PLAN_LIMIT_EXCEEDED or FEATURE_LOCKED — set only for plan/feature lock responses.</summary>
        public string? ErrorCode { get; init; }
        public string? CurrentPlan { get; init; }
        public IReadOnlyList<string>? RequiredPlans { get; init; }
        public string? UpgradeUrl { get; init; }
    }
}
