using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpareParts.Domain.Scanning;
using SpareParts.Infrastructure.Services;

namespace SpareParts.Api.Controllers;

[ApiController]
[Route("api/scans")]
[Authorize]
public sealed class ScansController : ControllerBase
{
    private readonly ScanLookupService _service;
    private readonly VisualPartSearchService _visualSearchService;

    public ScansController(
        ScanLookupService service,
        VisualPartSearchService visualSearchService)
    {
        _service = service;
        _visualSearchService = visualSearchService;
    }

    [HttpGet("resolve")]
    public ActionResult<IReadOnlyList<ScanLookupResultDto>> Resolve([FromQuery] string code)
        => Ok(_service.Resolve(code));

    [HttpPost("visual-search")]
    public async Task<ActionResult<VisualPartSearchResponseDto>> VisualSearch(
        IFormFile image,
        [FromForm] string? hint,
        [FromForm] int limit = 12,
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
