using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpareParts.Api.Infrastructure;
using SpareParts.Domain.Inventory;
using SpareParts.Domain.Scanning;
using SpareParts.Domain.WebCatalog;
using SpareParts.Infrastructure.Services;

namespace SpareParts.Api.Controllers
{
    [ApiController]
    [Route("api/web-catalog")]
    [Authorize(Policy = AuthorizationPolicies.WebAppUser)]
    public sealed class WebCatalogController : SparePartsControllerBase
    {
        private readonly WebCatalogService _service;
        private readonly PartRequestsService _partRequestsService;
        private readonly VisualPartSearchService _visualSearchService;

        public WebCatalogController(
            WebCatalogService service,
            PartRequestsService partRequestsService,
            VisualPartSearchService visualSearchService)
        {
            _service = service;
            _partRequestsService = partRequestsService;
            _visualSearchService = visualSearchService;
        }

        [HttpGet("parts")]
        [AllowAnonymous]
        public ActionResult<IReadOnlyList<WebCatalogPartDto>> GetAvailableParts(
            [FromQuery] string? search = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 100)
        {
            (page, pageSize) = NormalizePagination(page, pageSize);
            return Ok(_service.GetAvailableParts(search, page, pageSize));
        }

        [HttpPost("checkout")]
        public ActionResult<WebCheckoutResponse> Checkout([FromBody] WebCheckoutRequest request)
            => Ok(_service.Checkout(request, CurrentUserId));

        // Deliberately anonymous, unlike Checkout: this is a read-only price/promo-code preview (no
        // invoice, no stock movement) so a signed-out shopper can validate a code before signing in
        // to actually check out. See WebCatalogService.Quote / PriceCart for the tenant guard this
        // relies on to stay safe for anonymous callers.
        [HttpPost("checkout/quote")]
        [AllowAnonymous]
        public ActionResult<WebCheckoutQuoteResponse> QuoteCheckout([FromBody] WebCheckoutQuoteRequest request)
            => Ok(_service.Quote(request));

        [HttpPost("part-requests")]
        public ActionResult<int> CreatePartRequest([FromBody] CreatePartRequestItemRequest request)
            => Ok(_partRequestsService.Create(request, CurrentUserId));

        [HttpPost("visual-search")]
        [AllowAnonymous]
        public async Task<ActionResult<VisualPartSearchResponseDto>> VisualSearch(
            IFormFile image,
            [FromForm] string? hint,
            [FromForm] int limit = 8,
            CancellationToken cancellationToken = default)
        {
            if (image == null || image.Length == 0)
            {
                return BadRequest(new { message = "Image is required for visual part search." });
            }

            await using var stream = image.OpenReadStream();
            return Ok(await _visualSearchService.SearchAsync(
                stream,
                image.FileName,
                image.ContentType,
                hint,
                limit,
                cancellationToken));
        }
    }
}
