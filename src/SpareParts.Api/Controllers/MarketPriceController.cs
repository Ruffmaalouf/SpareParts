using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpareParts.Domain.Marketplace;
using SpareParts.Infrastructure.Services;

namespace SpareParts.Api.Controllers;

[ApiController]
[Route("api/market-price")]
[Authorize]
public sealed class MarketPriceController : SparePartsControllerBase
{
    private readonly MarketPriceIndexService _service;

    public MarketPriceController(MarketPriceIndexService service)
    {
        _service = service;
    }

    [HttpGet]
    public ActionResult<MarketPriceIndexDto> Get(
        [FromQuery] string partName,
        [FromQuery] string? make = null,
        [FromQuery] string? model = null,
        [FromQuery] int? year = null)
    {
        var result = _service.GetIndex(partName, make, model, year);
        if (result is null) return NotFound();
        return Ok(result);
    }
}
