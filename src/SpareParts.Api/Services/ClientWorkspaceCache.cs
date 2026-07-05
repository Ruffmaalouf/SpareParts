using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;
using SpareParts.Domain.Clients;

namespace SpareParts.Api.Services;

/// <summary>
/// Per-tenant, per-client in-memory cache for GET /api/clients/{id}/workspace — 20 s TTL
/// (Ignition Report 04 §04). Invalidated explicitly on any write to that customer's
/// invoices/payments (client version bump) and on tenant-wide money writes (tenant
/// version bump); superseded versions age out via TTL. Only the composed header read
/// model is cached — the paged child tabs always read live.
/// </summary>
public sealed class ClientWorkspaceCache
{
    public static readonly TimeSpan TimeToLive = TimeSpan.FromSeconds(20);

    private readonly IMemoryCache _cache;
    private readonly ConcurrentDictionary<int, long> _tenantVersions = new();
    private readonly ConcurrentDictionary<(int TenantId, int CustomerId), long> _clientVersions = new();

    public ClientWorkspaceCache(IMemoryCache cache)
    {
        _cache = cache;
    }

    public CachedApiResponse<ClientWorkspaceDto> GetOrAdd(int tenantId, int customerId, Func<ClientWorkspaceDto> factory)
    {
        var key = BuildKey(tenantId, customerId);
        if (_cache.TryGetValue<CachedApiResponse<ClientWorkspaceDto>>(key, out var cached) && cached is not null)
        {
            return cached;
        }

        var payload = factory();
        var entry = new CachedApiResponse<ClientWorkspaceDto>(
            payload,
            ApiResponseETagFactory.Compute($"wksp:{tenantId}:{customerId}", payload));
        _cache.Set(key, entry, TimeToLive);
        return entry;
    }

    public void InvalidateTenant(int tenantId)
        => _tenantVersions.AddOrUpdate(tenantId, 1, static (_, version) => version + 1);

    public void InvalidateClient(int tenantId, int customerId)
        => _clientVersions.AddOrUpdate((tenantId, customerId), 1, static (_, version) => version + 1);

    private string BuildKey(int tenantId, int customerId)
        => $"wksp:{tenantId}:v{_tenantVersions.GetValueOrDefault(tenantId)}:{customerId}:v{_clientVersions.GetValueOrDefault((tenantId, customerId))}";
}
