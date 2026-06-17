using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpareParts.Domain.Condition;
using SpareParts.Infrastructure.Services;

namespace SpareParts.Api.Controllers;

[ApiController]
[Route("api/condition-scanner")]
[Authorize]
public sealed class ConditionScannerController : SparePartsControllerBase
{
    private readonly ConditionScannerService _service;

    public ConditionScannerController(ConditionScannerService service)
    {
        _service = service;
    }

    [HttpPost("scan")]
    public ActionResult<ConditionCertificateDto> Scan([FromBody] CreateConditionScanRequest request)
        => Ok(_service.Scan(request, CurrentUserId));

    [HttpGet("part/{partId:int}")]
    public ActionResult<ConditionCertificateDto> GetLatest(int partId)
    {
        var dto = _service.GetLatestForPart(partId);
        if (dto is null) return NotFound();
        return Ok(dto);
    }
}
