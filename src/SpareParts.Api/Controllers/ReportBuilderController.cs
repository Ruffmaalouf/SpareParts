using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpareParts.Domain.Reports;
using SpareParts.Infrastructure.Services;

namespace SpareParts.Api.Controllers;

[ApiController]
[Route("api/reportbuilder")]
[Authorize]
public sealed class ReportBuilderController : SparePartsControllerBase
{
    private readonly ReportBuilderService _service;

    public ReportBuilderController(ReportBuilderService service)
    {
        _service = service;
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
        => Ok(_service.GetSecurityOptions(CurrentRoleName));

    [HttpGet("saved-reports")]
    public ActionResult<IEnumerable<ReportSavedReportSummaryDto>> GetSavedReports()
        => Ok(_service.GetSavedReports(CurrentUserId, CurrentRoleName));

    [HttpGet("saved-reports/{id:int}")]
    public ActionResult<ReportSavedReportDetailDto> GetSavedReport(int id)
        => Ok(_service.GetSavedReport(id, CurrentUserId, CurrentRoleName));

    [HttpPost("saved-reports")]
    public ActionResult<ReportSavedReportDetailDto> SaveReport([FromBody] SaveReportDefinitionRequest request)
        => Ok(_service.SaveReport(request, CurrentUserId, CurrentRoleName));

    [HttpDelete("saved-reports/{id:int}")]
    public IActionResult DeleteSavedReport(int id)
    {
        _service.DeleteSavedReport(id, CurrentUserId, CurrentRoleName);
        return NoContent();
    }

    [HttpPost("saved-reports/{id:int}/favorite")]
    public ActionResult<ReportSavedReportSummaryDto> SetFavorite(int id, [FromQuery] bool isFavorite = true)
        => Ok(_service.SetFavorite(id, isFavorite, CurrentUserId, CurrentRoleName));

    [HttpGet("background-runs")]
    public ActionResult<IEnumerable<BackgroundReportRunSummaryDto>> GetBackgroundRuns()
        => Ok(_service.GetBackgroundRuns(CurrentUserId, CurrentRoleName));

    [HttpPost("background-runs")]
    public ActionResult<BackgroundReportRunSummaryDto> QueueBackgroundRun([FromBody] QueueBackgroundReportRunRequest request)
        => Ok(_service.QueueBackgroundRun(request, CurrentUserId, CurrentRoleName));

    [HttpGet("background-runs/{id:int}/result")]
    public ActionResult<ReportResultDto> GetBackgroundRunResult(int id)
        => Ok(_service.GetBackgroundRunResult(id, CurrentUserId, CurrentRoleName));

    [HttpPost("links")]
    public ActionResult<ReportTableLinkDto> SaveLink([FromBody] SaveReportTableLinkRequest request)
        => Ok(_service.SaveLink(request));

    [HttpDelete("links/{id:int}")]
    public IActionResult DeleteLink(int id)
    {
        _service.DeleteLink(id);
        return NoContent();
    }

    [HttpPost("run")]
    public ActionResult<ReportResultDto> Run([FromBody] RunReportRequest request)
        => Ok(_service.RunReport(request));
}
