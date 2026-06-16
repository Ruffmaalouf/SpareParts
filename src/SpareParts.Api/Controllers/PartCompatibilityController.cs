using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpareParts.Domain.Compatibility;
using SpareParts.Infrastructure.Services;

namespace SpareParts.Api.Controllers;

[ApiController]
[Route("api/part-compatibility")]
[Authorize]
public sealed class PartCompatibilityController : SparePartsControllerBase
{
    private readonly PartCompatibilityService _service;

    public PartCompatibilityController(PartCompatibilityService service)
    {
        _service = service;
    }

    [HttpGet("{partId:int}")]
    public ActionResult<PartCompatibilityDto> GetByPartId(int partId)
    {
        var dto = _service.GetByPartId(partId);
        if (dto is null) return NotFound();
        return Ok(dto);
    }

    [HttpPost]
    public IActionResult Upsert([FromBody] CreatePartCompatibilityRequest request)
    {
        _service.Upsert(request, CurrentUserId);
        return NoContent();
    }

    [HttpGet("check")]
    public ActionResult<CompatibilityCheckResult> Check(
        [FromQuery] int partId,
        [FromQuery] string? make = null,
        [FromQuery] string? model = null,
        [FromQuery] int? year = null)
        => Ok(_service.Check(partId, make, model, year));
}
