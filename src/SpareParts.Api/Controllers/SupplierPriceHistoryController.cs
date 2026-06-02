using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpareParts.Domain.Purchases;
using SpareParts.Infrastructure.Services;

namespace SpareParts.Api.Controllers;

[ApiController]
[Route("api/supplier-price-history")]
[Authorize]
public sealed class SupplierPriceHistoryController : SparePartsControllerBase
{
    private readonly SupplierPriceHistoryService _service;

    public SupplierPriceHistoryController(SupplierPriceHistoryService service)
    {
        _service = service;
    }

    [HttpGet("parts/{partId:int}")]
    public ActionResult<IReadOnlyList<SupplierPriceHistoryDto>> GetHistory(int partId)
        => Ok(_service.GetHistory(partId));

    [HttpGet("parts/{partId:int}/comparison")]
    public ActionResult<SupplierPriceComparisonDto> GetComparison(int partId)
    {
        var result = _service.GetComparison(partId);
        return result == null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public IActionResult RecordPrice([FromBody] RecordSupplierPriceRequest req)
    {
        _service.RecordPrice(req, CurrentUserId);
        return NoContent();
    }
}
