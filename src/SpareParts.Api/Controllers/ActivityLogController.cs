using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpareParts.Domain.AuditLog;
using SpareParts.Infrastructure.Services;

namespace SpareParts.Api.Controllers;

[ApiController]
[Route("api/activity-log")]
[Authorize]
public sealed class ActivityLogController : SparePartsControllerBase
{
    private readonly ActivityLogService _service;

    public ActivityLogController(ActivityLogService service)
    {
        _service = service;
    }

    [HttpGet]
    public ActionResult<IReadOnlyList<ActivityLogDto>> GetLogs(
        [FromQuery] string? entityType = null,
        [FromQuery] int? entityId = null,
        [FromQuery] int? userId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 100)
        => Ok(_service.GetLogs(entityType, entityId, userId, page, pageSize));

    [HttpPost]
    public IActionResult Log([FromBody] LogActivityRequest req)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userName = User.Identity?.Name;
        _service.Log(req, CurrentUserId, userName, ip);
        return NoContent();
    }
}
