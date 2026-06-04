using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpareParts.Api.Infrastructure;
using SpareParts.Domain.ExcelImport;
using SpareParts.Infrastructure.Services;

namespace SpareParts.Api.Controllers;

[ApiController]
[Route("api/excelimport")]
[Authorize(Policy = AuthorizationPolicies.Admin)]
public sealed class ExcelImportController : SparePartsControllerBase
{
    private readonly ExcelImportService _service;

    public ExcelImportController(ExcelImportService service)
    {
        _service = service;
    }

    [HttpGet("targets")]
    public ActionResult<IEnumerable<ExcelImportTableDto>> GetTargets()
        => Ok(_service.GetTargets());

    [HttpPost("rows")]
    public IActionResult ImportRow([FromBody] ExcelImportRowRequest request)
    {
        _service.ImportRow(request, CurrentUserId);
        return NoContent();
    }
}
