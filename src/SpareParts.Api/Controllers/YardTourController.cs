using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpareParts.Domain.YardTour;
using SpareParts.Infrastructure.Services;

namespace SpareParts.Api.Controllers;

[ApiController]
[Route("api/yard-tours")]
[Authorize]
public sealed class YardTourController : SparePartsControllerBase
{
    private readonly YardTourService _service;

    public YardTourController(YardTourService service)
    {
        _service = service;
    }

    [HttpGet]
    public IActionResult GetAll([FromQuery] YardTourStatus? status)
        => Ok(_service.GetAll(status));

    [HttpGet("{id:int}")]
    public IActionResult GetById(int id)
    {
        var dto = _service.GetById(id);
        if (dto is null) return NotFound();
        return Ok(dto);
    }

    [HttpPost]
    public ActionResult<YardTourDto> Create([FromBody] CreateYardTourRequest request)
        => Ok(_service.Create(request, CurrentUserId));

    [HttpPost("{id:int}/interest")]
    public IActionResult SubmitInterest(int id, [FromBody] SubmitYardTourInterestRequest request)
    {
        _service.SubmitInterest(id, request);
        return NoContent();
    }

    [HttpPatch("{id:int}/status")]
    public IActionResult UpdateStatus(int id, [FromBody] YardTourStatus newStatus)
    {
        _service.UpdateStatus(id, newStatus);
        return NoContent();
    }
}
