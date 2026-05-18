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

    public ScansController(ScanLookupService service)
    {
        _service = service;
    }

    [HttpGet("resolve")]
    public ActionResult<IReadOnlyList<ScanLookupResultDto>> Resolve([FromQuery] string code)
        => Ok(_service.Resolve(code));
}
