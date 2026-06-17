using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpareParts.Domain.Pricing;
using SpareParts.Infrastructure.Services;

namespace SpareParts.Api.Controllers;

[ApiController]
[Route("api/new-vs-used")]
[Authorize]
public sealed class NewVsUsedController : SparePartsControllerBase
{
    private readonly NewVsUsedService _service;

    public NewVsUsedController(NewVsUsedService service)
    {
        _service = service;
    }

    [HttpGet("{partId:int}")]
    public ActionResult<NewVsUsedDto> GetComparison(int partId)
    {
        var dto = _service.GetComparison(partId);
        if (dto is null) return NotFound();
        return Ok(dto);
    }

    [HttpPut("prices")]
    public IActionResult UpdateNewPrices([FromBody] UpdateNewPricesRequest request)
    {
        _service.UpdateNewPrices(request, CurrentUserId);
        return NoContent();
    }
}
