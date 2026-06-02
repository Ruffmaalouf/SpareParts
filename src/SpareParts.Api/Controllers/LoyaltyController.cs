using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpareParts.Domain.Loyalty;
using SpareParts.Infrastructure.Services;

namespace SpareParts.Api.Controllers;

[ApiController]
[Route("api/loyalty")]
[Authorize]
public sealed class LoyaltyController : SparePartsControllerBase
{
    private readonly LoyaltyService _service;

    public LoyaltyController(LoyaltyService service)
    {
        _service = service;
    }

    [HttpGet("customers/{customerId:int}")]
    public ActionResult<CustomerLoyaltyDto> GetCustomerLoyalty(int customerId)
    {
        var result = _service.GetCustomerLoyalty(customerId);
        return result == null ? NotFound() : Ok(result);
    }

    [HttpGet("customers/top")]
    public ActionResult<IReadOnlyList<CustomerLoyaltyDto>> GetTopCustomers([FromQuery] int top = 20)
        => Ok(_service.GetTopCustomers(top));

    [HttpGet("customers/{customerId:int}/transactions")]
    public ActionResult<IReadOnlyList<LoyaltyTransactionDto>> GetTransactions(int customerId)
        => Ok(_service.GetTransactions(customerId));

    [HttpPost("points")]
    public IActionResult AddPoints([FromBody] AddLoyaltyPointsRequest req)
    {
        _service.AddPoints(req, CurrentUserId);
        return NoContent();
    }

    [HttpPost("redeem")]
    public IActionResult RedeemPoints([FromBody] RedeemLoyaltyPointsRequest req)
    {
        _service.RedeemPoints(req, CurrentUserId);
        return NoContent();
    }
}
