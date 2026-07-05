namespace SpareParts.Infrastructure.Interfaces;

/// <summary>
/// Write-path invalidation hook for the per-tenant dashboard-summary and client-workspace
/// response caches (Ignition Report 04 §04). Implementations must be cheap (in-memory,
/// microseconds) — this is called from inside request paths that have just committed a
/// sale/payment/purchase write, never from a background job.
/// </summary>
public interface IDashboardCacheInvalidator
{
    /// <summary>Drops every cached dashboard summary (and workspace entry) for the tenant.</summary>
    void InvalidateTenant(int tenantId);

    /// <summary>Drops the cached workspace read model for one customer of the tenant.</summary>
    void InvalidateClient(int tenantId, int customerId);
}
