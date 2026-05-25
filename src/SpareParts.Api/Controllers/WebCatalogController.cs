using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpareParts.Api.Infrastructure;
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

        public WebCatalogController(WebCatalogService service)
        {
            _service = service;
        }

        [HttpGet("parts")]
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
    }
}
