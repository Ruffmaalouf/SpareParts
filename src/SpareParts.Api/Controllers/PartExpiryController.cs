using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpareParts.Domain.Inventory;
using SpareParts.Infrastructure.Services;

namespace SpareParts.Api.Controllers;

[ApiController]
[Route("api/parts/expiry")]
[Authorize]
public sealed class PartExpiryController : SparePartsControllerBase
{
    private readonly PartExpiryService _service;

    public PartExpiryController(PartExpiryService service)
    {
        _service = service;
    }

    [HttpGet("alerts")]
    public ActionResult<IReadOnlyList<PartExpiryAlertDto>> GetAlerts([FromQuery] int daysAhead = 90)
        => Ok(_service.GetExpiryAlerts(daysAhead));

    [HttpPut("{partId:int}")]
    public IActionResult SetExpiry(int partId, [FromBody] SetPartExpiryRequest req)
    {
        _service.SetExpiry(partId, req, CurrentUserId);
        return NoContent();
    }
}
