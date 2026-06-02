using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpareParts.Domain.Forecasting;
using SpareParts.Infrastructure.Services;

namespace SpareParts.Api.Controllers;

[ApiController]
[Route("api/reorder")]
[Authorize]
public sealed class ReorderController : SparePartsControllerBase
{
    private readonly ReorderAnalysisService _service;

    public ReorderController(ReorderAnalysisService service)
    {
        _service = service;
    }

    [HttpGet("rules")]
    public ActionResult<IReadOnlyList<ReorderRuleDto>> GetRules()
        => Ok(_service.GetRules());

    [HttpPut("rules")]
    public IActionResult UpsertRule([FromBody] UpsertReorderRuleRequest req)
    {
        _service.UpsertRule(req, CurrentUserId);
        return NoContent();
    }

    [HttpDelete("rules/{partId:int}")]
    public IActionResult DeleteRule(int partId)
    {
        _service.DeleteRule(partId);
        return NoContent();
    }

    [HttpGet("suggestions")]
    public ActionResult<IReadOnlyList<ReorderSuggestionDto>> GetSuggestions()
        => Ok(_service.GetSuggestions());
}
