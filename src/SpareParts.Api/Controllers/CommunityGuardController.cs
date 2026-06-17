using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpareParts.Domain.Trust;
using SpareParts.Infrastructure.Services;

namespace SpareParts.Api.Controllers;

[ApiController]
[Route("api/community-guard")]
[Authorize]
public sealed class CommunityGuardController : SparePartsControllerBase
{
    private readonly CommunityGuardService _service;

    public CommunityGuardController(CommunityGuardService service)
    {
        _service = service;
    }

    [HttpGet]
    public IActionResult GetReports([FromQuery] ReportStatus? status, [FromQuery] ReportTargetType? targetType)
        => Ok(_service.GetReports(status, targetType));

    [HttpPost]
    public ActionResult<ContentReportDto> Submit([FromBody] CreateContentReportRequest request)
        => Ok(_service.Submit(request, CurrentUserId));

    [HttpPatch("{id:int}/status")]
    public IActionResult UpdateStatus(int id, [FromBody] ReportStatus newStatus)
    {
        _service.UpdateStatus(id, newStatus);
        return NoContent();
    }
}
