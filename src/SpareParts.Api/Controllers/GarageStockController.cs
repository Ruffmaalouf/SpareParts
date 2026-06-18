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

    [HttpPut("{id:int}")]
    public IActionResult Update(int id, [FromBody] UpdateGarageStockItemRequest request)
    {
        _service.Update(id, request, CurrentUserId);
        return NoContent();
    }

    [HttpPut("{id:int}/quantity")]
    public IActionResult UpdateQuantity(int id, [FromBody] UpdateGarageStockQuantityRequest request)
    {
        _service.UpdateQuantity(id, request.Quantity, CurrentUserId);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public IActionResult Deactivate(int id)
    {
        _service.Deactivate(id, CurrentUserId);
        return NoContent();
    }
}
