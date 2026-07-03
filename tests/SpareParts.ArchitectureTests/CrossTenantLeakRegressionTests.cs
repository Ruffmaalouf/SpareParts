using System.Collections;
using System.Reflection;
using SpareParts.ArchitectureTests.TestDoubles;
using SpareParts.Infrastructure.Interfaces;
using SpareParts.Infrastructure.Services;

namespace SpareParts.ArchitectureTests;

/// <summary>
/// Regression coverage for the critical C1 fix: anonymous/unresolved-tenant callers of
/// public-catalog services must never see cross-tenant data. Before the fix, an anonymous
/// request (TenantResolutionMiddleware never ran, so TenantId defaulted to 0) was
/// indistinguishable from an authenticated SuperAdmin's "see all tenants" request, because the
/// underlying SQL used "@TenantId = 0 OR ..." as the filter. Both WebCatalogService and
/// VisualPartSearchService now guard: IsSuperAdmin == false AND TenantId &lt;= 0 short-circuits
/// to an empty result BEFORE any database call is made.
/// </summary>
public class CrossTenantLeakRegressionTests
{
    [Fact]
    public void WebCatalogService_GetAvailableParts_ShouldReturnEmpty_ForUnresolvedAnonymousTenant_WithoutTouchingDatabase()
    {
        // TenantId <= 0 and IsSuperAdmin == false is the exact "unresolved anonymous" shape that
        // caused the C1 leak. The connection factory throws if it is ever asked for a connection,
        // proving the guard short-circuits before any SQL runs (not just that the SQL happens to
        // return no rows).
        var tenantContext = new TenantContext { TenantId = 0, IsSuperAdmin = false, IsResolved = false };
        var service = new WebCatalogService(new ThrowIfCalledSqlConnectionFactory(), salesService: null!, tenantContext);

        var result = service.GetAvailableParts(search: null, page: 1, pageSize: 20);

        Assert.Empty(result);
    }

    [Fact]
    public void WebCatalogService_GetAvailableParts_ShouldReturnEmpty_ForNegativeTenantId_WithoutTouchingDatabase()
    {
        var tenantContext = new TenantContext { TenantId = -1, IsSuperAdmin = false, IsResolved = false };
        var service = new WebCatalogService(new ThrowIfCalledSqlConnectionFactory(), salesService: null!, tenantContext);

        var result = service.GetAvailableParts(search: null, page: 1, pageSize: 20);

        Assert.Empty(result);
    }

    [Fact]
    public void WebCatalogService_GetAvailableParts_ShouldProceedPastGuard_ForResolvedTenant()
    {
        // A resolved, positive TenantId must NOT be short-circuited: the guard should let the
        // request through to the database layer. We can't run the SQL-Server-flavored query
        // against a fake connection, so we assert the "proceeds past the guard" behavior by
        // observing that CreateConnection *is* invoked (the throwing factory trips), which is
        // the opposite outcome of the anonymous-guard tests above.
        var tenantContext = new TenantContext { TenantId = 7, IsSuperAdmin = false, IsResolved = true };
        var service = new WebCatalogService(new ThrowIfCalledSqlConnectionFactory(), salesService: null!, tenantContext);

        Assert.Throws<InvalidOperationException>(() => service.GetAvailableParts(search: null, page: 1, pageSize: 20));
    }

    [Fact]
    public void WebCatalogService_GetAvailableParts_ShouldProceedPastGuard_ForSuperAdmin_EvenWithTenantIdZero()
    {
        // A SuperAdmin legitimately gets TenantId == 0 to mean "all tenants" — that is the one
        // case where TenantId <= 0 must NOT be treated as anonymous/unresolved.
        var tenantContext = new TenantContext { TenantId = 0, IsSuperAdmin = true, IsResolved = true };
        var service = new WebCatalogService(new ThrowIfCalledSqlConnectionFactory(), salesService: null!, tenantContext);

        Assert.Throws<InvalidOperationException>(() => service.GetAvailableParts(search: null, page: 1, pageSize: 20));
    }

    [Fact]
    public void VisualPartSearchService_QueryPartCandidates_ShouldReturnEmpty_ForUnresolvedAnonymousTenant_WithoutTouchingDatabase()
    {
        // QueryPartCandidates is private (only reachable via the public async SearchAsync, which
        // requires an image stream and can call out to the configured vision provider). Invoking
        // it directly via reflection lets us assert the guard logic in isolation, matching the
        // same "no database access" proof used for WebCatalogService above.
        var tenantContext = new TenantContext { TenantId = 0, IsSuperAdmin = false, IsResolved = false };
        var service = new VisualPartSearchService(
            new ThrowIfCalledSqlConnectionFactory(),
            new HttpClient(),
            new OpenAiOptions(),
            tenantContext);

        var candidates = InvokeQueryPartCandidates(service);

        Assert.Empty(candidates);
    }

    [Fact]
    public void VisualPartSearchService_QueryPartCandidates_ShouldReturnEmpty_ForNegativeTenantId_WithoutTouchingDatabase()
    {
        var tenantContext = new TenantContext { TenantId = -5, IsSuperAdmin = false, IsResolved = false };
        var service = new VisualPartSearchService(
            new ThrowIfCalledSqlConnectionFactory(),
            new HttpClient(),
            new OpenAiOptions(),
            tenantContext);

        var candidates = InvokeQueryPartCandidates(service);

        Assert.Empty(candidates);
    }

    [Fact]
    public void VisualPartSearchService_QueryPartCandidates_ShouldProceedPastGuard_ForResolvedTenant()
    {
        // As with WebCatalogService, a real database is required to exercise the full query for
        // a resolved tenant/SuperAdmin (the SQL is SQL-Server-flavored and cannot run against the
        // SQLite fixtures used elsewhere in this project — see IntegrationTests, which require
        // Docker/SQL Server, for full end-to-end coverage). Here we assert only that the guard
        // lets the resolved-tenant case proceed to the data access layer.
        var tenantContext = new TenantContext { TenantId = 3, IsSuperAdmin = false, IsResolved = true };
        var service = new VisualPartSearchService(
            new ThrowIfCalledSqlConnectionFactory(),
            new HttpClient(),
            new OpenAiOptions(),
            tenantContext);

        var exception = Assert.Throws<TargetInvocationException>(() => InvokeQueryPartCandidates(service));
        Assert.IsType<InvalidOperationException>(exception.InnerException);
    }

    [Fact]
    public void VisualPartSearchService_QueryPartCandidates_ShouldProceedPastGuard_ForSuperAdmin_EvenWithTenantIdZero()
    {
        var tenantContext = new TenantContext { TenantId = 0, IsSuperAdmin = true, IsResolved = true };
        var service = new VisualPartSearchService(
            new ThrowIfCalledSqlConnectionFactory(),
            new HttpClient(),
            new OpenAiOptions(),
            tenantContext);

        var exception = Assert.Throws<TargetInvocationException>(() => InvokeQueryPartCandidates(service));
        Assert.IsType<InvalidOperationException>(exception.InnerException);
    }

    private static ICollection InvokeQueryPartCandidates(VisualPartSearchService service)
    {
        var method = typeof(VisualPartSearchService).GetMethod(
            "QueryPartCandidates",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);

        return (ICollection)method!.Invoke(service, null)!;
    }
}
