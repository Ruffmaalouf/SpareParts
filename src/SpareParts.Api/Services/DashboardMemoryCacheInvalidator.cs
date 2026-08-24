namespace SpareParts.Api.Services;

/// <summary>
/// IMemoryCache-backed implementation of the write-path invalidation hook
/// (Ignition Report 04 §04). A committed sale/payment/purchase drops the tenant's
/// dashboard summaries (and workspace headers, since balances moved); a client-scoped
/// write additionally drops that customer's workspace entry. Costs microseconds —
/// safe to call inline from the committing request.
/// </summary>
public sealed class DashboardMemoryCacheInvalidator : IDashboardCacheInvalidator
{
    private readonly DashboardSummaryCache _dashboardCache;
    private readonly ClientWorkspaceCache _workspaceCache;

    public DashboardMemoryCacheInvalidator(DashboardSummaryCache dashboardCache, ClientWorkspaceCache workspaceCache)
    {
        _dashboardCache = dashboardCache;
        _workspaceCache = workspaceCache;
    }

    public void InvalidateTenant(int tenantId)
    {
        _dashboardCache.InvalidateTenant(tenantId);
        _workspaceCache.InvalidateTenant(tenantId);
    }

    public void InvalidateClient(int tenantId, int customerId)
        => _workspaceCache.InvalidateClient(tenantId, customerId);
}
