using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SpareParts.Api.Hosting;
using SpareParts.Api.Services;
using SpareParts.Domain.Clients;
using SpareParts.Infrastructure.Services;

namespace SpareParts.Api.Controllers
{
    /// <summary>
    /// The Ignition Client Workspace resource (Report 04 §05–§06): a customer-360 header
    /// read model plus independently paged child tabs and the client-rail typeahead.
    /// All tenant-ownership checks run inside ClientWorkspaceService BEFORE any child
    /// query; missing and cross-tenant ids return an identical 404. Additive — no
    /// existing customer/invoice/quote route is changed.
    /// </summary>
    [ApiController]
    [Route("api/clients")]
    [Authorize]
    public sealed class ClientWorkspaceController : SparePartsControllerBase
    {
        private readonly ClientWorkspaceService _service;
        private readonly ClientWorkspaceCache _cache;

        public ClientWorkspaceController(ClientWorkspaceService service, ClientWorkspaceCache cache)
        {
            _service = service;
            _cache = cache;
        }

        [HttpGet("{id:int}/workspace")]
        public ActionResult<ClientWorkspaceDto> GetWorkspace(int id)
        {
            var entry = _cache.GetOrAdd(CurrentTenantId, id, () => _service.GetWorkspace(id));

            if (RequestETagMatches(entry.ETag))
            {
                return StatusCode(StatusCodes.Status304NotModified);
            }

            Response.Headers.ETag = entry.ETag;
            return Ok(entry.Payload);
        }

        [HttpGet("{id:int}/invoices")]
        public ActionResult<IReadOnlyList<ClientInvoiceDto>> GetInvoices(
            int id,
            [FromQuery] int? page = null,
            [FromQuery] int? pageSize = null,
            [FromQuery] string? sort = null,
            [FromQuery] string? status = null)
        {
            var (normalizedPage, normalizedPageSize) = ClientWorkspaceService.NormalizeChildPagination(page, pageSize);
            var result = _service.GetInvoices(id, normalizedPage, normalizedPageSize, sort, status);
            ApplyPaginationHeaders(normalizedPage, normalizedPageSize, result.TotalCount);
            return Ok(result.Items);
        }

        [HttpGet("{id:int}/payments")]
        public ActionResult<IReadOnlyList<ClientPaymentDto>> GetPayments(
            int id,
            [FromQuery] int? page = null,
            [FromQuery] int? pageSize = null)
        {
            var (normalizedPage, normalizedPageSize) = ClientWorkspaceService.NormalizeChildPagination(page, pageSize);
            var result = _service.GetPayments(id, normalizedPage, normalizedPageSize);
            ApplyPaginationHeaders(normalizedPage, normalizedPageSize, result.TotalCount);
            return Ok(result.Items);
        }

        [HttpGet("{id:int}/quotes")]
        public ActionResult<IReadOnlyList<ClientQuoteDto>> GetQuotes(
            int id,
            [FromQuery] int? page = null,
            [FromQuery] int? pageSize = null,
            [FromQuery] string? status = null)
        {
            var (normalizedPage, normalizedPageSize) = ClientWorkspaceService.NormalizeChildPagination(page, pageSize);
            var result = _service.GetQuotes(id, normalizedPage, normalizedPageSize, status);
            ApplyPaginationHeaders(normalizedPage, normalizedPageSize, result.TotalCount);
            return Ok(result.Items);
        }

        [HttpGet("{id:int}/part-requests")]
        public ActionResult<IReadOnlyList<ClientPartRequestDto>> GetPartRequests(
            int id,
            [FromQuery] int? page = null,
            [FromQuery] int? pageSize = null,
            [FromQuery] string? status = null)
        {
            var (normalizedPage, normalizedPageSize) = ClientWorkspaceService.NormalizeChildPagination(page, pageSize);
            var result = _service.GetPartRequests(id, normalizedPage, normalizedPageSize, status);
            ApplyPaginationHeaders(normalizedPage, normalizedPageSize, result.TotalCount);
            return Ok(result.Items);
        }

        [HttpGet("{id:int}/timeline")]
        public ActionResult<IReadOnlyList<ClientTimelineEventDto>> GetTimeline(
            int id,
            [FromQuery] int? page = null,
            [FromQuery] int? pageSize = null,
            [FromQuery] string? types = null)
        {
            var (normalizedPage, normalizedPageSize) = ClientWorkspaceService.NormalizeChildPagination(page, pageSize);
            var typeFilter = string.IsNullOrWhiteSpace(types)
                ? null
                : types.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var result = _service.GetTimeline(id, normalizedPage, normalizedPageSize, typeFilter);
            ApplyPaginationHeaders(normalizedPage, normalizedPageSize, result.TotalCount);
            return Ok(result.Items);
        }

        [HttpGet("search")]
        [EnableRateLimiting(SparePartsApiComposition.ClientSearchRateLimitPolicy)]
        public ActionResult<IReadOnlyList<ClientSearchResultDto>> Search(
            [FromQuery] string? q = null,
            [FromQuery] int? take = null)
            => Ok(_service.Search(q, ClientWorkspaceService.NormalizeSearchTake(take)));

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
