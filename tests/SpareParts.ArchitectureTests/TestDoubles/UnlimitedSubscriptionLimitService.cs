namespace SpareParts.ArchitectureTests.TestDoubles;

internal sealed class UnlimitedSubscriptionLimitService : ISubscriptionLimitService
{
    public static UnlimitedSubscriptionLimitService Instance { get; } = new();

    private UnlimitedSubscriptionLimitService()
    {
    }

    public string GetActivePackageCode(int tenantId) => "FREE";

    public bool HasFeature(int tenantId, string featureCode) => true;

    public void EnsureFeatureEnabled(int tenantId, string featureCode, string featureDisplayName)
    {
    }

    public int GetLimitValue(int tenantId, string limitCode) => -1;

    public void EnsureWithinLimit(int tenantId, string limitCode, int currentCount, string limitDisplayName, int additional = 1)
    {
    }

    public int GetMonthlyUsage(int tenantId, string counterCode) => 0;

    public int EnsureAndIncrementMonthlyUsage(int tenantId, string counterCode, string limitDisplayName, int additional = 1) => additional;
}
