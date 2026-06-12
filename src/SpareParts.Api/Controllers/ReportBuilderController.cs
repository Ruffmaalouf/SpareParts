using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpareParts.Api.Infrastructure;
using SpareParts.Domain.Pricing;
using SpareParts.Domain.Reports;
using SpareParts.Infrastructure.Services;

namespace SpareParts.Api.Controllers;

[ApiController]
[Route("api/reportbuilder")]
[Authorize(Policy = AuthorizationPolicies.AdminOrManager)]
public sealed class ReportBuilderController : SparePartsControllerBase
{
    private readonly ReportBuilderService _service;
    private readonly ISubscriptionLimitService _subscriptionLimitService;

    public ReportBuilderController(ReportBuilderService service, ISubscriptionLimitService subscriptionLimitService)
    {
        _service = service;
        _subscriptionLimitService = subscriptionLimitService;
    }

    [HttpGet("tables")]
    public ActionResult<IEnumerable<ReportTableDto>> GetTables()
        => Ok(_service.GetTables());

    [HttpGet("columns")]
    public ActionResult<IEnumerable<ReportColumnDto>> GetColumns([FromQuery] string tableKey)
        => Ok(_service.GetColumns(tableKey));

    [HttpGet("links")]
    public ActionResult<IEnumerable<ReportTableLinkDto>> GetLinks()
        => Ok(_service.GetLinks());

    [HttpGet("join-graph")]
    public ActionResult<ReportJoinGraphDto> GetJoinGraph()
        => Ok(_service.GetJoinGraph());

    [HttpGet("schema")]
    public ActionResult<ReportSchemaInspectorDto> GetSchemaInspector([FromQuery] string tableKey)
        => Ok(_service.GetSchemaInspector(tableKey));

    [HttpGet("security-options")]
    public ActionResult<ReportSecurityOptionsDto> GetSecurityOptions()
        => Ok(_service.GetSecurityOptions(CurrentRoleId));

    [HttpGet("saved-reports")]
    public ActionResult<IEnumerable<ReportSavedReportSummaryDto>> GetSavedReports()
        => Ok(_service.GetSavedReports(CurrentUserId, CurrentRoleId));

    [HttpGet("saved-reports/{id:int}")]
    public ActionResult<ReportSavedReportDetailDto> GetSavedReport(int id)
        => Ok(_service.GetSavedReport(id, CurrentUserId, CurrentRoleId));

    [HttpPost("saved-reports")]
    public ActionResult<ReportSavedReportDetailDto> SaveReport([FromBody] SaveReportDefinitionRequest request)
        => Ok(_service.SaveReport(request, CurrentUserId, CurrentRoleId));

    [HttpDelete("saved-reports/{id:int}")]
    public IActionResult DeleteSavedReport(int id)
    {
        _service.DeleteSavedReport(id, CurrentUserId, CurrentRoleId);
        return NoContent();
    }

    [HttpPost("saved-reports/{id:int}/favorite")]
    public ActionResult<ReportSavedReportSummaryDto> SetFavorite(int id, [FromQuery] bool isFavorite = true)
        => Ok(_service.SetFavorite(id, isFavorite, CurrentUserId, CurrentRoleId));

    [HttpGet("background-runs")]
    public ActionResult<IEnumerable<BackgroundReportRunSummaryDto>> GetBackgroundRuns()
        => Ok(_service.GetBackgroundRuns(CurrentUserId, CurrentRoleId));

    [HttpPost("background-runs")]
    public ActionResult<BackgroundReportRunSummaryDto> QueueBackgroundRun([FromBody] QueueBackgroundReportRunRequest request)
    {
        _subscriptionLimitService.EnsureFeatureEnabled(CurrentTenantId, FeatureCode.ReportsAdvanced, "Advanced/Scheduled Reports");
        return Ok(_service.QueueBackgroundRun(request, CurrentUserId, CurrentRoleId));
    }

    [HttpGet("background-runs/{id:int}/result")]
    public ActionResult<ReportResultDto> GetBackgroundRunResult(int id)
        => Ok(_service.GetBackgroundRunResult(id, CurrentUserId, CurrentRoleId));

    [HttpPost("links")]
    [Authorize(Policy = AuthorizationPolicies.Admin)]
    public ActionResult<ReportTableLinkDto> SaveLink([FromBody] SaveReportTableLinkRequest request)
        => Ok(_service.SaveLink(request, CurrentRoleId));

    [HttpDelete("links/{id:int}")]
    [Authorize(Policy = AuthorizationPolicies.Admin)]
    public IActionResult DeleteLink(int id)
    {
        _service.DeleteLink(id, CurrentRoleId);
        return NoContent();
    }

    [HttpPost("run")]
    public ActionResult<ReportResultDto> Run([FromBody] RunReportRequest request)
    {
        _subscriptionLimitService.EnsureFeatureEnabled(CurrentTenantId, FeatureCode.ReportsBasic, "Reports");
        return Ok(_service.RunReport(request));
    }
}
