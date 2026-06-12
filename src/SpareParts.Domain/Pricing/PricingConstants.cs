namespace SpareParts.Domain.Pricing;

/// <summary>Stable codes for tenant-facing pricing packages (seeded by PricingPackagesMigration).</summary>
public static class PackageCode
{
    public const string Free = "FREE";
    public const string Basic = "BASIC";
    public const string Pro = "PRO";
    public const string Enterprise = "ENTERPRISE";

    public static readonly IReadOnlyList<string> All = [Free, Basic, Pro, Enterprise];
}

/// <summary>Stable codes for plan-gated features. Presence + IsEnabled in PackageFeatures controls access.</summary>
public static class FeatureCode
{
    public const string PartListing = "PART_LISTING";
    public const string PartImages = "PART_IMAGES";
    public const string BuyerLeads = "BUYER_LEADS";
    public const string MobileSellerApp = "MOBILE_SELLER_APP";
    public const string ReportsBasic = "REPORTS_BASIC";
    public const string ReportsAdvanced = "REPORTS_ADVANCED";
    public const string MultiBranch = "MULTI_BRANCH";
    public const string BulkImport = "BULK_IMPORT";
    public const string AiTitle = "AI_TITLE";
    public const string AiDescription = "AI_DESCRIPTION";
    public const string AiPriceSuggestion = "AI_PRICE_SUGGESTION";
    public const string PromotedListings = "PROMOTED_LISTINGS";
    public const string FeaturedParts = "FEATURED_PARTS";
    public const string ApiAccess = "API_ACCESS";
    public const string PrioritySupport = "PRIORITY_SUPPORT";
    public const string CustomIntegrations = "CUSTOM_INTEGRATIONS";
    public const string WhiteLabel = "WHITE_LABEL";

    public static readonly IReadOnlyList<string> All =
    [
        PartListing, PartImages, BuyerLeads, MobileSellerApp, ReportsBasic, ReportsAdvanced,
        MultiBranch, BulkImport, AiTitle, AiDescription, AiPriceSuggestion, PromotedListings,
        FeaturedParts, ApiAccess, PrioritySupport, CustomIntegrations, WhiteLabel
    ];
}

/// <summary>
/// Stable codes for numeric plan limits / monthly counters. A PackageLimits.LimitValue of -1 means "unlimited".
/// </summary>
public static class LimitCode
{
    public const string UsersCount = "USERS_COUNT";
    public const string BranchesCount = "BRANCHES_COUNT";
    public const string ActivePartsCount = "ACTIVE_PARTS_COUNT";
    public const string ImagesPerPart = "IMAGES_PER_PART";
    public const string BuyerLeadsMonthly = "BUYER_LEADS_MONTHLY";
    public const string PromotedPartsMonthly = "PROMOTED_PARTS_MONTHLY";
    public const string FeaturedPartsMonthly = "FEATURED_PARTS_MONTHLY";
    public const string ApiCallsMonthly = "API_CALLS_MONTHLY";

    /// <summary>Limit value used to mean "no cap" in PackageLimits.LimitValue.</summary>
    public const int Unlimited = -1;

    public static readonly IReadOnlyList<string> All =
    [
        UsersCount, BranchesCount, ActivePartsCount, ImagesPerPart, BuyerLeadsMonthly,
        PromotedPartsMonthly, FeaturedPartsMonthly, ApiCallsMonthly
    ];

    /// <summary>Limit codes that represent a monthly counter reset by the subscription maintenance job.</summary>
    public static readonly IReadOnlyList<string> MonthlyCounters =
    [
        BuyerLeadsMonthly, PromotedPartsMonthly, FeaturedPartsMonthly, ApiCallsMonthly
    ];
}

/// <summary>Stable codes for pluggable payment providers.</summary>
public static class PaymentProviderCode
{
    public const string Manual = "MANUAL";
    public const string Test = "TEST";
    public const string Stripe = "STRIPE";

    public static readonly IReadOnlyList<string> All = [Manual, Test, Stripe];
}

public enum BillingCycle
{
    Monthly = 1,
    Yearly = 2
}

public enum SubscriptionStatus
{
    Trialing = 1,
    Active = 2,
    PastDue = 3,
    Cancelled = 4,
    Expired = 5
}

public enum PaymentStatus
{
    Pending = 1,
    Succeeded = 2,
    Failed = 3,
    Refunded = 4
}

public enum InvoiceStatus
{
    Draft = 1,
    Issued = 2,
    Paid = 3,
    Void = 4
}

public enum PaymentTransactionStatus
{
    Initiated = 1,
    Pending = 2,
    Succeeded = 3,
    Failed = 4
}

public enum WebhookProcessingStatus
{
    Pending = 1,
    Processed = 2,
    Failed = 3,
    Ignored = 4
}

/// <summary>Error codes returned in the body of a 403 plan/feature lock response.</summary>
public static class PlanLockErrorCode
{
    public const string PlanLimitExceeded = "PLAN_LIMIT_EXCEEDED";
    public const string FeatureLocked = "FEATURE_LOCKED";
}
