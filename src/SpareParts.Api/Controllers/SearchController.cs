using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpareParts.Domain.Search;
using SpareParts.Infrastructure.Services;

namespace SpareParts.Api.Controllers;

[ApiController]
[Route("api/search")]
[Authorize]
public sealed class SearchController : ControllerBase
{
    private readonly SmartSearchService _service;

    public SearchController(SmartSearchService service)
    {
        _service = service;
    }

    [HttpGet]
    public ActionResult<SmartSearchResponseDto> Search(
        [FromQuery] string? q,
        [FromQuery] int limit = 30)
        => Ok(_service.Search(q, limit));
}
