using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpareParts.Domain.BusinessPartners;
using SpareParts.Infrastructure.Services;

namespace SpareParts.Api.Controllers;

[ApiController]
[Route("api/customer-pricing")]
[Authorize]
public sealed class CustomerPricingController : SparePartsControllerBase
{
    private readonly CustomerPriceTierService _service;

    public CustomerPricingController(CustomerPriceTierService service)
    {
        _service = service;
    }

    [HttpGet]
    public ActionResult<IReadOnlyList<CustomerPriceTierDto>> GetAll()
        => Ok(_service.GetAll());

    [HttpPut("{customerId:int}/tier")]
    public IActionResult UpdateTier(int customerId, [FromBody] UpdateCustomerPriceTierRequest req)
    {
        _service.UpdateTier(customerId, req.PriceTier, CurrentUserId);
        return NoContent();
    }

    [HttpGet("resolve")]
    public ActionResult<decimal> ResolvePartPrice([FromQuery] int partId, [FromQuery] int? customerId)
        => Ok(_service.ResolvePartPrice(partId, customerId));
}
