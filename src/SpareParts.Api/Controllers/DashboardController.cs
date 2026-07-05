using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpareParts.Api.Services;
using SpareParts.Domain.Dashboard;
using SpareParts.Infrastructure.Services;

namespace SpareParts.Api.Controllers
{
    /// <summary>
    /// GET /api/dashboard/summary — the composed Ignition dashboard read model
    /// (Report 04 §02–§04). Thin by design: aggregation lives in DashboardService;
    /// this controller only handles the per-tenant cache and weak-ETag revalidation.
    /// Additive — /api/owner-cockpit is untouched and keeps serving WPF/mobile.
    /// </summary>
    [ApiController]
    [Route("api/dashboard")]
    [Authorize]
    public sealed class DashboardController : SparePartsControllerBase
    {
        private readonly DashboardService _service;
        private readonly DashboardSummaryCache _cache;

        public DashboardController(DashboardService service, DashboardSummaryCache cache)
        {
            _service = service;
            _cache = cache;
        }

        [HttpGet("summary")]
        public ActionResult<DashboardSummaryDto> GetSummary([FromQuery] DateTime? businessDate = null)
        {
            var date = (businessDate ?? DateTime.Today).Date;
            var tenantId = CurrentTenantId;

            // Cache hit + matching If-None-Match answers 304 with no DB round trip;
            // a cache miss runs the composed load exactly once per tenant per TTL window.
            var entry = _cache.GetOrAdd(tenantId, date, () => _service.GetSummary(date));

            if (RequestETagMatches(entry.ETag))
            {
                return StatusCode(StatusCodes.Status304NotModified);
            }

            Response.Headers.ETag = entry.ETag;
            return Ok(entry.Payload);
        }

        private bool RequestETagMatches(string etag)
        {
            foreach (var headerValue in Request.Headers.IfNoneMatch)
            {
                if (headerValue is not null && headerValue.Contains(etag, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
