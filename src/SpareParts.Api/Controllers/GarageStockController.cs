using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpareParts.Domain.GarageStock;
using SpareParts.Infrastructure.Services;

namespace SpareParts.Api.Controllers;

[ApiController]
[Route("api/garage-stock")]
[Authorize]
public sealed class GarageStockController : SparePartsControllerBase
{
    private readonly GarageStockService _service;

    public GarageStockController(GarageStockService service)
    {
        _service = service;
    }

    [HttpGet]
    public ActionResult<IEnumerable<GarageStockItemDto>> GetAll([FromQuery] bool lowStockOnly = false)
        => Ok(_service.GetAll(lowStockOnly));

    [HttpPost]
    public ActionResult<int> Create([FromBody] CreateGarageStockItemRequest request)
        => Ok(_service.Create(request, CurrentUserId));

    [HttpPut("{id:int}/quantity")]
    public IActionResult UpdateQuantity(int id, [FromQuery] int quantity)
    {
        _service.UpdateQuantity(id, quantity, CurrentUserId);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public IActionResult Deactivate(int id)
    {
        _service.Deactivate(id, CurrentUserId);
        return NoContent();
    }
}
