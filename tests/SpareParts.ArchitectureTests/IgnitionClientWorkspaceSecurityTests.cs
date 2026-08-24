using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Caching.Memory;
using SpareParts.Api.Controllers;
using SpareParts.Api.Hosting;
using SpareParts.Api.Services;
using SpareParts.ArchitectureTests.TestDoubles;
using SpareParts.Domain.Clients;
using SpareParts.Infrastructure.Services;

namespace SpareParts.ArchitectureTests;

/// <summary>
/// Ignition QA gate — Client Workspace security + contract tests
/// (Report 06 §07 client-workspace security block · Report 04 §05–§07, §09).
///
/// Highest-priority focus (CLAUDE.md security areas): the ClientWorkspaceService
/// enumeration-prevention guarantee. These unit-level tests prove the pre-database guard:
/// an unresolved/anonymous tenant can never enumerate, and every read surfaces the SAME
/// uniform NotFoundException. The cross-tenant-vs-nonexistent "identical 404" against a real
/// database (two seeded tenants) is proven end-to-end in
/// SpareParts.IntegrationTests.ClientWorkspaceSqlServerIntegrationTests.
/// </summary>
public class IgnitionClientWorkspaceSecurityTests
{
    private const string ExpectedNotFoundMessage = "Customer not found.";
    private const string TenantIdClaimType = "tenant_id";

    // ── Enumeration guard: unresolved/anonymous tenant → uniform 404, no DB touched ──────

    [Fact]
    public void EveryWorkspaceRead_ForUnresolvedAnonymousTenant_ThrowsUniformNotFound_WithoutTouchingDatabase()
    {
        // TenantId <= 0 && !IsSuperAdmin is the "unresolved anonymous" shape behind the C1 leak.
        // The throwing factory proves GuardResolvedTenant short-circuits before any SQL runs.
        var service = BuildService(new TenantContext { TenantId = 0, IsSuperAdmin = false, IsResolved = false });

        foreach (var (name, invoke) in WorkspaceReads())
        {
            var ex = Record.Exception(() => invoke(service));
            Assert.True(ex is NotFoundException, $"{name} should throw NotFoundException for an unresolved tenant, got {ex?.GetType().Name ?? "no exception"}.");
            Assert.Equal(ExpectedNotFoundMessage, ex!.Message);
        }
    }

    [Fact]
    public void EveryWorkspaceRead_ForNegativeTenantId_ThrowsUniformNotFound_WithoutTouchingDatabase()
    {
        var service = BuildService(new TenantContext { TenantId = -3, IsSuperAdmin = false, IsResolved = false });

        foreach (var (name, invoke) in WorkspaceReads())
        {
            var ex = Record.Exception(() => invoke(service));
            Assert.True(ex is NotFoundException, $"{name} should throw NotFoundException for a negative tenant id, got {ex?.GetType().Name ?? "no exception"}.");
            Assert.Equal(ExpectedNotFoundMessage, ex!.Message);
        }
    }

    [Fact]
    public void EveryWorkspaceRead_ForResolvedTenant_ProceedsPastGuard_ToTheDataLayer()
    {
        // A resolved, positive tenant must NOT be short-circuited: the guard lets it through to
        // the DB layer, where the throwing factory trips with InvalidOperationException — the
        // opposite outcome to the NotFoundException guard above (mirrors CrossTenantLeakRegressionTests).
        var service = BuildService(new TenantContext { TenantId = 9, IsSuperAdmin = false, IsResolved = true });

        foreach (var (name, invoke) in WorkspaceReads())
        {
            var ex = Record.Exception(() => invoke(service));
            Assert.True(ex is InvalidOperationException, $"{name} should proceed past the guard for a resolved tenant, got {ex?.GetType().Name ?? "no exception"}.");
        }
    }

    [Fact]
    public void EveryWorkspaceRead_ForSuperAdminWithTenantZero_ProceedsPastGuard()
    {
        // SuperAdmin legitimately gets TenantId == 0 meaning "all tenants" — the one case where
        // TenantId <= 0 must NOT be treated as anonymous/unresolved (Report 04 §09 SuperAdmin note).
        var service = BuildService(new TenantContext { TenantId = 0, IsSuperAdmin = true, IsResolved = true });

        foreach (var (name, invoke) in WorkspaceReads())
        {
            var ex = Record.Exception(() => invoke(service));
            Assert.True(ex is InvalidOperationException, $"{name} should proceed past the guard for a SuperAdmin, got {ex?.GetType().Name ?? "no exception"}.");
        }
    }

    // ── Typeahead search: lenient, tenant-guarded, and never touches the DB when it must not ──

    [Fact]
    public void Search_WithShortQuery_ReturnsEmpty_WithoutTouchingDatabase()
    {
        var service = BuildService(new TenantContext { TenantId = 9, IsSuperAdmin = false, IsResolved = true });

        Assert.Empty(service.Search("a", take: 10));
        Assert.Empty(service.Search("  ", take: 10));
        Assert.Empty(service.Search(null, take: 10));
    }

    [Fact]
    public void Search_ForUnresolvedAnonymousTenant_ReturnsEmpty_WithoutTouchingDatabase()
    {
        var service = BuildService(new TenantContext { TenantId = 0, IsSuperAdmin = false, IsResolved = false });

        Assert.Empty(service.Search("brake", take: 10));
    }

    [Fact]
    public void Search_ForResolvedTenantWithValidQuery_ProceedsPastGuard_ToTheDataLayer()
    {
        var service = BuildService(new TenantContext { TenantId = 9, IsSuperAdmin = false, IsResolved = true });

        Assert.Throws<InvalidOperationException>(() => service.Search("brake", take: 10));
    }

    // ── Auth coverage + rate-limit application (Report 06 §07) ───────────────────────────

    [Fact]
    public void ClientWorkspaceController_ShouldCarryClassLevelAuthorize_AndTheClientsRoute()
    {
        Assert.NotNull(typeof(ClientWorkspaceController).GetCustomAttribute<AuthorizeAttribute>());

        var route = typeof(ClientWorkspaceController).GetCustomAttribute<RouteAttribute>();
        Assert.NotNull(route);
        Assert.Equal("api/clients", route!.Template);
    }

    [Fact]
    public void EveryClientWorkspaceAction_ShouldBeCoveredByAuthorization()
    {
        // Class-level [Authorize] covers all actions; none may carry [AllowAnonymous] (Report 06 §07:
        // "all child routes carry [Authorize] and tenant filters").
        var actions = typeof(ClientWorkspaceController)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Where(m => !m.IsSpecialName);

        foreach (var action in actions)
        {
            Assert.Empty(action.GetCustomAttributes<AllowAnonymousAttribute>(inherit: true));
        }
    }

    [Fact]
    public void SearchAction_ShouldApplyThePerTenantClientSearchRateLimitPolicy()
    {
        var search = typeof(ClientWorkspaceController).GetMethod(nameof(ClientWorkspaceController.Search));
        Assert.NotNull(search);

        var rateLimit = search!.GetCustomAttribute<EnableRateLimitingAttribute>();
        Assert.NotNull(rateLimit);
        Assert.Equal(SparePartsApiComposition.ClientSearchRateLimitPolicy, rateLimit!.PolicyName);
        Assert.Equal("client-search", SparePartsApiComposition.ClientSearchRateLimitPolicy);
    }

    // ── Payload shape + zero-leakage (Report 04 §05, §09) ────────────────────────────────

    [Fact]
    public void ClientWorkspaceDto_ShouldExposeExactly_TheReport04Section05HeaderContract()
    {
        var expected = new Dictionary<string, Type>(StringComparer.Ordinal)
        {
            ["CustomerId"] = typeof(int),
            ["Name"] = typeof(string),
            ["Phone"] = typeof(string),
            ["Email"] = typeof(string),
            ["Address"] = typeof(string),
            ["IsActive"] = typeof(bool),
            ["CurrencyCode"] = typeof(string),
            ["Balance"] = typeof(ClientBalanceDto),
            ["Kpis"] = typeof(ClientWorkspaceKpisDto),
            ["Vehicles"] = typeof(List<ClientVehicleDto>),
            ["LastActivityAt"] = typeof(DateTime?)
        };

        var actual = typeof(ClientWorkspaceDto)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .ToDictionary(p => p.Name, p => p.PropertyType, StringComparer.Ordinal);

        Assert.Equal(
            expected.Keys.OrderBy(x => x, StringComparer.Ordinal),
            actual.Keys.OrderBy(x => x, StringComparer.Ordinal));
        foreach (var (name, type) in expected)
        {
            Assert.Equal(type, actual[name]);
        }
    }

    [Theory]
    [InlineData(typeof(ClientWorkspaceDto))]
    [InlineData(typeof(ClientInvoiceDto))]
    [InlineData(typeof(ClientPaymentDto))]
    [InlineData(typeof(ClientQuoteDto))]
    [InlineData(typeof(ClientPartRequestDto))]
    [InlineData(typeof(ClientTimelineEventDto))]
    [InlineData(typeof(ClientSearchResultDto))]
    public void ClientWorkspacePayloads_MustNotLeakTenantOrDbInternals(Type payloadType)
        => IgnitionPayloadLeakageAssert.AssertNoLeakage(payloadType);

    // ── Pagination contract (Report 04 §06 — child tabs paged, never unbounded) ──────────

    [Theory]
    [InlineData(null, null, 1, 25)]     // defaults: page 1, DefaultPageSize
    [InlineData(0, 0, 1, 1)]            // clamps below-range to minimums
    [InlineData(3, 1000, 3, 100)]      // clamps page size to MaxPageSize (100)
    [InlineData(-5, 50, 1, 50)]        // negative page floored to 1, valid size kept
    public void NormalizeChildPagination_ShouldClampWithinContractBounds(int? page, int? pageSize, int expectedPage, int expectedSize)
    {
        var (normalizedPage, normalizedPageSize) = ClientWorkspaceService.NormalizeChildPagination(page, pageSize);

        Assert.Equal(expectedPage, normalizedPage);
        Assert.Equal(expectedSize, normalizedPageSize);
    }

    [Theory]
    [InlineData(null, 10)]  // default take
    [InlineData(0, 1)]      // clamp to min
    [InlineData(999, 25)]   // clamp to SearchMaxTake
    public void NormalizeSearchTake_ShouldClampWithinContractBounds(int? take, int expected)
        => Assert.Equal(expected, ClientWorkspaceService.NormalizeSearchTake(take));

    [Fact]
    public void ApplyPaginationHeaders_ShouldEmitTheExistingPaginationHeaderContract()
    {
        // The child routes surface their page window through the SAME headers every other paged
        // route uses (SparePartsControllerBase.ApplyPaginationHeaders). This proves the mechanism;
        // the end-to-end TotalCount wiring is covered by the SQL Server integration test.
        var probe = new PaginationHeaderProbeController
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        probe.Emit(page: 2, pageSize: 25, totalCount: 137);

        Assert.Equal("2", probe.Response.Headers["X-Page"]);
        Assert.Equal("25", probe.Response.Headers["X-Page-Size"]);
        Assert.Equal("137", probe.Response.Headers["X-Total-Count"]);
    }

    // ── Client-workspace 304 revalidation at the controller seam (no DB) ─────────────────

    [Fact]
    public void GetWorkspace_WithMatchingIfNoneMatch_ShouldReturn304_WithoutTouchingTheService()
    {
        var cache = new ClientWorkspaceCache(new MemoryCache(new MemoryCacheOptions()));
        var seeded = cache.GetOrAdd(7, 55, () => new ClientWorkspaceDto { CustomerId = 55, Name = "Acme" });

        var controller = new ClientWorkspaceController(service: null!, cache)
        {
            ControllerContext = new ControllerContext { HttpContext = BuildTenantHttpContext(7, seeded.ETag) }
        };

        var result = controller.GetWorkspace(55);

        var status = Assert.IsType<StatusCodeResult>(result.Result);
        Assert.Equal(StatusCodes.Status304NotModified, status.StatusCode);
    }

    // ── helpers ──────────────────────────────────────────────────────────────────────────

    private static ClientWorkspaceService BuildService(ITenantContext context)
        => new(new ThrowIfCalledSqlConnectionFactory(), context);

    private static IEnumerable<(string Name, Action<ClientWorkspaceService> Invoke)> WorkspaceReads()
    {
        const int anyCustomerId = 123;
        yield return (nameof(ClientWorkspaceService.GetWorkspace), s => s.GetWorkspace(anyCustomerId));
        yield return (nameof(ClientWorkspaceService.GetInvoices), s => s.GetInvoices(anyCustomerId, 1, 25, null, null));
        yield return (nameof(ClientWorkspaceService.GetPayments), s => s.GetPayments(anyCustomerId, 1, 25));
        yield return (nameof(ClientWorkspaceService.GetQuotes), s => s.GetQuotes(anyCustomerId, 1, 25, null));
        yield return (nameof(ClientWorkspaceService.GetPartRequests), s => s.GetPartRequests(anyCustomerId, 1, 25, null));
        yield return (nameof(ClientWorkspaceService.GetTimeline), s => s.GetTimeline(anyCustomerId, 1, 25, null));
    }

    private static DefaultHttpContext BuildTenantHttpContext(int tenantId, string ifNoneMatch)
    {
        var context = new DefaultHttpContext();
        context.User = new ClaimsPrincipal(new ClaimsIdentity(
            new[] { new Claim(TenantIdClaimType, tenantId.ToString()) }, "Test"));
        context.Request.Headers["If-None-Match"] = ifNoneMatch;
        return context;
    }

    private sealed class PaginationHeaderProbeController : SparePartsControllerBase
    {
        public void Emit(int page, int pageSize, int totalCount) => ApplyPaginationHeaders(page, pageSize, totalCount);
    }
}
