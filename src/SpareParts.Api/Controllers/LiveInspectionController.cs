using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpareParts.Domain.Inspection;
using SpareParts.Infrastructure.Services;

namespace SpareParts.Api.Controllers;

[ApiController]
[Route("api/live-inspection")]
[Authorize]
public sealed class LiveInspectionController : SparePartsControllerBase
{
    private readonly LiveInspectionService _service;

    public LiveInspectionController(LiveInspectionService service)
    {
        _service = service;
    }

    [HttpGet]
    public IActionResult GetAll([FromQuery] InspectionStatus? status)
        => Ok(_service.GetAll(status));

    [HttpGet("{id:int}")]
    public IActionResult GetById(int id)
    {
        var dto = _service.GetById(id);
        if (dto is null) return NotFound();
        return Ok(dto);
    }

    [HttpPost]
    public ActionResult<InspectionRequestDto> Create([FromBody] CreateInspectionRequest request)
        => Ok(_service.Create(request, CurrentUserId));

    [HttpPatch("{id:int}/status")]
    public IActionResult UpdateStatus(int id, [FromBody] UpdateInspectionStatusRequest request)
    {
        _service.UpdateStatus(id, request);
        return NoContent();
    }
}
