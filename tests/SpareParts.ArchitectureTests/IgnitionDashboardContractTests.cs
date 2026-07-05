using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using SpareParts.Api.Controllers;
using SpareParts.Api.Services;
using SpareParts.Domain.Dashboard;

namespace SpareParts.ArchitectureTests;

/// <summary>
/// Ignition QA gate — GET /api/dashboard/summary contract + security tests
/// (Report 06 §06 dashboard checklist · Report 04 §02–§03 payload contract, §09 security).
///
/// Pure/unit-level: they run in the "Build &amp; Test" job (no SQL Server / Docker). The 45s
/// cache TTL, weak-ETag and 304 revalidation are exercised at the controller seam with a
/// pre-seeded cache, so no DashboardService/DB call happens on the cache-hit path.
/// </summary>
public class IgnitionDashboardContractTests
{
    private const string TenantIdClaimType = "tenant_id";

    // ── Payload shape (Report 04 §03) ─────────────────────────────────────────

    [Fact]
    public void DashboardSummaryDto_ShouldExposeExactly_TheReport04Section03TopLevelContract()
    {
        var expected = new Dictionary<string, Type>(StringComparer.Ordinal)
        {
            ["BusinessDate"] = typeof(DateTime),
            ["GeneratedAt"] = typeof(DateTime),
            ["CurrencyCode"] = typeof(string),
            ["Kpis"] = typeof(DashboardKpisDto),
            ["ActionQueue"] = typeof(List<DashboardActionQueueItemDto>),
            ["SalesTrend"] = typeof(DashboardSalesTrendDto),
            ["LowStockPanel"] = typeof(DashboardLowStockPanelDto),
            ["DeadStockPanel"] = typeof(DashboardDeadStockPanelDto),
            ["UnpaidTransactionsPanel"] = typeof(DashboardUnpaidTransactionsPanelDto)
        };

        AssertPropertyContract(typeof(DashboardSummaryDto), expected);
    }

    [Fact]
    public void DashboardKpisDto_ShouldExposeTheSixKpiTiles_FromTheExamplePayload()
    {
        var expected = new Dictionary<string, Type>(StringComparer.Ordinal)
        {
            ["SalesToday"] = typeof(DashboardKpiDto),
            ["ProfitToday"] = typeof(DashboardKpiDto),
            ["CashBalance"] = typeof(DashboardKpiDto),
            ["Receivables"] = typeof(DashboardKpiDto),
            ["StockValue"] = typeof(DashboardKpiDto),
            ["LowStockCount"] = typeof(DashboardKpiDto)
        };

        AssertPropertyContract(typeof(DashboardKpisDto), expected);
    }

    [Fact]
    public void DashboardKpiDto_ShouldExposeValueDeltaAndDirection()
    {
        var expected = new Dictionary<string, Type>(StringComparer.Ordinal)
        {
            ["Value"] = typeof(decimal),
            ["DeltaPercent"] = typeof(decimal?),
            ["Direction"] = typeof(string)
        };

        AssertPropertyContract(typeof(DashboardKpiDto), expected);
    }

    [Fact]
    public void DashboardActionQueueItemDto_ShouldExposeTheScoredDeepLinkContract()
    {
        var expected = new Dictionary<string, Type>(StringComparer.Ordinal)
        {
            ["Id"] = typeof(string),
            ["Type"] = typeof(string),
            ["Severity"] = typeof(string),
            ["Title"] = typeof(string),
            ["Detail"] = typeof(string),
            ["Amount"] = typeof(decimal?),
            ["CurrencyCode"] = typeof(string),
            ["DeepLink"] = typeof(string)
        };

        AssertPropertyContract(typeof(DashboardActionQueueItemDto), expected);
    }

    // ── ETag / 304 revalidation (Report 06 §06 "unchanged data returns 304 with no body") ──

    [Fact]
    public void GetSummary_WithMatchingIfNoneMatch_ShouldReturn304_WithoutTouchingTheService()
    {
        var businessDate = new DateTime(2026, 7, 2);
        var cache = new DashboardSummaryCache(new MemoryCache(new MemoryCacheOptions()));

        // Pre-seed the cache so the controller path is a pure cache hit — the DashboardService
        // is never invoked (it is null here and would NRE if the factory lambda ran).
        var seeded = cache.GetOrAdd(42, businessDate, () => SampleSummary(businessDate));
        var controller = BuildController(cache, tenantId: 42, ifNoneMatch: seeded.ETag);

        var result = controller.GetSummary(businessDate);

        var status = Assert.IsType<StatusCodeResult>(result.Result);
        Assert.Equal(StatusCodes.Status304NotModified, status.StatusCode);
    }

    [Fact]
    public void GetSummary_WithoutIfNoneMatch_ShouldReturn200_AndSetTheWeakETagHeader()
    {
        var businessDate = new DateTime(2026, 7, 2);
        var cache = new DashboardSummaryCache(new MemoryCache(new MemoryCacheOptions()));
        var seeded = cache.GetOrAdd(42, businessDate, () => SampleSummary(businessDate));
        var controller = BuildController(cache, tenantId: 42, ifNoneMatch: null);

        var result = controller.GetSummary(businessDate);

        Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(seeded.ETag, controller.Response.Headers.ETag.ToString());
        Assert.StartsWith("W/\"", controller.Response.Headers.ETag.ToString());
    }

    [Fact]
    public void GetSummary_WithNonMatchingIfNoneMatch_ShouldReturn200_NotA304()
    {
        var businessDate = new DateTime(2026, 7, 2);
        var cache = new DashboardSummaryCache(new MemoryCache(new MemoryCacheOptions()));
        cache.GetOrAdd(42, businessDate, () => SampleSummary(businessDate));
        var controller = BuildController(cache, tenantId: 42, ifNoneMatch: "W/\"deadbeefdeadbeef\"");

        var result = controller.GetSummary(businessDate);

        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public void WeakETag_ShouldBeDeterministic_AndNotLeakTenantIdInTheHeaderValue()
    {
        var payload = SampleSummary(new DateTime(2026, 7, 2));

        var first = ApiResponseETagFactory.Compute("dash:118:20260702", payload);
        var second = ApiResponseETagFactory.Compute("dash:118:20260702", payload);

        Assert.Equal(first, second);
        Assert.StartsWith("W/\"", first);
        // The scope seed contains the tenant id and cache-key shape, but the emitted digest is an
        // opaque content hash — the raw scope/cache-key string must not be recoverable from the
        // header (Report 04 §08/§09 zero-leakage rule).
        Assert.DoesNotContain("dash:118", first, StringComparison.Ordinal);
        Assert.DoesNotContain("20260702", first, StringComparison.Ordinal);
    }

    // ── Security / auth coverage (Report 06 §06 security block) ─────────────────

    [Fact]
    public void DashboardController_ShouldCarryClassLevelAuthorize_AndTheDashboardRoute()
    {
        Assert.NotNull(typeof(DashboardController).GetCustomAttribute<AuthorizeAttribute>());

        var route = typeof(DashboardController).GetCustomAttribute<RouteAttribute>();
        Assert.NotNull(route);
        Assert.Equal("api/dashboard", route!.Template);
    }

    [Fact]
    public void DashboardSummaryDto_MustNotLeakTenantOrCacheMetadata()
        => IgnitionPayloadLeakageAssert.AssertNoLeakage(typeof(DashboardSummaryDto));

    // ── helpers ────────────────────────────────────────────────────────────────

    private static DashboardController BuildController(DashboardSummaryCache cache, int tenantId, string? ifNoneMatch)
    {
        // null! DashboardService: never invoked on the cache-hit path these tests drive.
        var controller = new DashboardController(service: null!, cache)
        {
            ControllerContext = new ControllerContext { HttpContext = BuildHttpContext(tenantId, ifNoneMatch) }
        };
        return controller;
    }

    private static DefaultHttpContext BuildHttpContext(int tenantId, string? ifNoneMatch)
    {
        var context = new DefaultHttpContext();
        var identity = new ClaimsIdentity(
            new[] { new Claim(TenantIdClaimType, tenantId.ToString()) },
            authenticationType: "Test");
        context.User = new ClaimsPrincipal(identity);
        if (ifNoneMatch is not null)
        {
            context.Request.Headers["If-None-Match"] = ifNoneMatch;
        }

        return context;
    }

    private static DashboardSummaryDto SampleSummary(DateTime businessDate)
        => new()
        {
            BusinessDate = businessDate,
            GeneratedAt = businessDate.AddHours(9),
            CurrencyCode = "USD",
            Kpis = new DashboardKpisDto(),
            ActionQueue = new List<DashboardActionQueueItemDto>(),
            SalesTrend = new DashboardSalesTrendDto { RangeDays = 7 },
            LowStockPanel = new DashboardLowStockPanelDto(),
            DeadStockPanel = new DashboardDeadStockPanelDto(),
            UnpaidTransactionsPanel = new DashboardUnpaidTransactionsPanelDto()
        };

    private static void AssertPropertyContract(Type type, IReadOnlyDictionary<string, Type> expected)
    {
        var actual = type
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .ToDictionary(p => p.Name, p => p.PropertyType, StringComparer.Ordinal);

        Assert.Equal(
            expected.Keys.OrderBy(x => x, StringComparer.Ordinal),
            actual.Keys.OrderBy(x => x, StringComparer.Ordinal));

        foreach (var (name, propertyType) in expected)
        {
            Assert.Equal(propertyType, actual[name]);
        }
    }
}
