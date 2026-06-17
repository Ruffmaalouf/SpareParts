using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpareParts.Domain.Mechanic;
using SpareParts.Infrastructure.Services;

namespace SpareParts.Api.Controllers;

[ApiController]
[Route("api/mechanic-trust")]
[Authorize]
public sealed class MechanicTrustController : SparePartsControllerBase
{
    private readonly MechanicTrustService _service;

    public MechanicTrustController(MechanicTrustService service)
    {
        _service = service;
    }

    [HttpGet]
    public IActionResult GetAll()
        => Ok(_service.GetAll());

    [HttpGet("user/{userId:int}")]
    public IActionResult GetByUser(int userId)
    {
        var dto = _service.GetByUserId(userId);
        if (dto is null) return NotFound();
        return Ok(dto);
    }

    [HttpPost]
    public ActionResult<MechanicProfileDto> CreateOrUpdate([FromBody] CreateMechanicProfileRequest request)
        => Ok(_service.CreateOrUpdate(request, CurrentUserId));

    [HttpPost("user/{userId:int}/verify")]
    public IActionResult Verify(int userId)
    {
        _service.Verify(userId);
        return NoContent();
    }
}
