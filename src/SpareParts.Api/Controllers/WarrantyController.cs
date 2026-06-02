using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpareParts.Domain.Warranty;
using SpareParts.Infrastructure.Services;

namespace SpareParts.Api.Controllers;

[ApiController]
[Route("api/warranty")]
[Authorize]
public sealed class WarrantyController : SparePartsControllerBase
{
    private readonly WarrantyService _service;

    public WarrantyController(WarrantyService service)
    {
        _service = service;
    }

    [HttpGet]
    public ActionResult<IReadOnlyList<WarrantyClaimDto>> GetClaims([FromQuery] string? status = null)
        => Ok(_service.GetClaims(status));

    [HttpGet("{id:int}")]
    public ActionResult<WarrantyClaimDto> GetClaim(int id)
    {
        var result = _service.GetClaim(id);
        return result == null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public ActionResult<int> CreateClaim([FromBody] CreateWarrantyClaimRequest req)
        => Ok(_service.CreateClaim(req, CurrentUserId));

    [HttpPut("{id:int}/resolve")]
    public IActionResult ResolveClaim(int id, [FromBody] ResolveWarrantyClaimRequest req)
    {
        _service.ResolveClaim(id, req, CurrentUserId);
        return NoContent();
    }
}
