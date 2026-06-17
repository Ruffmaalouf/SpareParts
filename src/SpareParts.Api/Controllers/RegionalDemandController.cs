using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpareParts.Infrastructure.Services;

namespace SpareParts.Api.Controllers;

[ApiController]
[Route("api/regional-demand")]
[Authorize]
public sealed class RegionalDemandController : SparePartsControllerBase
{
    private readonly RegionalDemandService _service;

    public RegionalDemandController(RegionalDemandService service)
    {
        _service = service;
    }

    [HttpGet]
    public IActionResult GetByRegion([FromQuery] string? partName)
        => Ok(_service.GetByRegion(partName));

    [HttpGet("top")]
    public IActionResult GetTopDemanded([FromQuery] int top = 20)
        => Ok(_service.GetTopDemandedParts(top));
}
