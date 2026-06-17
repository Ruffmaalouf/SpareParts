using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpareParts.Domain.Reports;
using SpareParts.Infrastructure.Services;

namespace SpareParts.Api.Controllers;

[ApiController]
[Route("api/price-report")]
[Authorize]
public sealed class PriceReportController : SparePartsControllerBase
{
    private readonly PriceReportService _service;

    public PriceReportController(PriceReportService service)
    {
        _service = service;
    }

    [HttpPost]
    public ActionResult<PriceReportDto> Generate([FromBody] PriceReportFilter filter)
        => Ok(_service.Generate(filter));
}
