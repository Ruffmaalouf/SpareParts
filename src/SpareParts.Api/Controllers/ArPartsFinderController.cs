using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpareParts.Domain.Ar;
using SpareParts.Infrastructure.Services;

namespace SpareParts.Api.Controllers;

[ApiController]
[Route("api/ar-finder")]
[Authorize]
public sealed class ArPartsFinderController : SparePartsControllerBase
{
    private readonly ArPartsFinderService _service;

    public ArPartsFinderController(ArPartsFinderService service)
    {
        _service = service;
    }

    [HttpPost("scan")]
    public ActionResult<ArScanResult> Scan([FromBody] ArScanRequest request)
        => Ok(_service.Scan(request));
}
