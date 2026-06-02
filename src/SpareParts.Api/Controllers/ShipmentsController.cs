using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpareParts.Domain.Shipments;
using SpareParts.Infrastructure.Services;

namespace SpareParts.Api.Controllers;

[ApiController]
[Route("api/shipments")]
[Authorize]
public sealed class ShipmentsController : SparePartsControllerBase
{
    private readonly ShipmentsService _service;

    public ShipmentsController(ShipmentsService service)
    {
        _service = service;
    }

    [HttpGet]
    public ActionResult<IReadOnlyList<ShipmentDto>> GetShipments([FromQuery] string? status = null)
        => Ok(_service.GetShipments(status));

    [HttpGet("{id:int}")]
    public ActionResult<ShipmentDto> GetShipment(int id)
    {
        var result = _service.GetShipment(id);
        return result == null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public ActionResult<int> CreateShipment([FromBody] CreateShipmentRequest req)
        => Ok(_service.CreateShipment(req, CurrentUserId));

    [HttpPut("{id:int}/status")]
    public IActionResult UpdateStatus(int id, [FromBody] UpdateShipmentStatusRequest req)
    {
        _service.UpdateStatus(id, req, CurrentUserId);
        return NoContent();
    }
}
