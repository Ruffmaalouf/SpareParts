using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;
using SpareParts.Domain.Dashboard;

namespace SpareParts.Api.Services;

/// <summary>
/// Per-tenant in-memory cache for GET /api/dashboard/summary — 45 s TTL keyed by
/// tenant + business date, wrapping the composed DTO, not the individual SQL results
/// (Ignition Report 04 §04). Invalidation bumps a per-tenant version so every cached
/// business date for that tenant is dropped at once; stale versions simply age out via TTL.
/// IMemoryCache is correct for the current single-host IIS/YARP topology; a scale-out
/// deployment swaps the IDashboardCacheInvalidator implementation, not the callers.
/// </summary>
public sealed class DashboardSummaryCache
{
    public static readonly TimeSpan TimeToLive = TimeSpan.FromSeconds(45);

    private readonly IMemoryCache _cache;
    private readonly ConcurrentDictionary<int, long> _tenantVersions = new();

    public DashboardSummaryCache(IMemoryCache cache)
    {
        _cache = cache;
    }

    public CachedApiResponse<DashboardSummaryDto> GetOrAdd(
        int tenantId,
        DateTime businessDate,
        Func<DashboardSummaryDto> factory)
    {
        var key = BuildKey(tenantId, businessDate);
        if (_cache.TryGetValue<CachedApiResponse<DashboardSummaryDto>>(key, out var cached) && cached is not null)
        {
            return cached;
        }

        var payload = factory();
        var entry = new CachedApiResponse<DashboardSummaryDto>(
            payload,
            ApiResponseETagFactory.Compute($"dash:{tenantId}:{businessDate:yyyyMMdd}", payload));
        _cache.Set(key, entry, TimeToLive);
        return entry;
    }

    public void InvalidateTenant(int tenantId)
        => _tenantVersions.AddOrUpdate(tenantId, 1, static (_, version) => version + 1);

    private string BuildKey(int tenantId, DateTime businessDate)
        => $"dash:{tenantId}:v{_tenantVersions.GetValueOrDefault(tenantId)}:{businessDate:yyyyMMdd}";
}
