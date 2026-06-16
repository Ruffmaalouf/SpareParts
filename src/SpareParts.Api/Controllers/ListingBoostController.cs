using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpareParts.Domain.Marketplace;
using SpareParts.Infrastructure.Services;

namespace SpareParts.Api.Controllers;

[ApiController]
[Route("api/listing-boosts")]
[Authorize]
public sealed class ListingBoostController : SparePartsControllerBase
{
    private readonly ListingBoostService _service;

    public ListingBoostController(ListingBoostService service)
    {
        _service = service;
    }

    [HttpGet]
    public ActionResult<IEnumerable<ListingBoostDto>> GetAll([FromQuery] ListingBoostStatus? status = null)
        => Ok(_service.GetAll(status));

    [HttpPost]
    public ActionResult<int> Create([FromBody] CreateListingBoostRequest request)
        => Ok(_service.Create(request, CurrentUserId));

    [HttpPut("{id:int}/cancel")]
    public IActionResult Cancel(int id)
    {
        _service.Cancel(id, CurrentUserId);
        return NoContent();
    }

    [HttpGet("parts/{partId:int}/active")]
    public ActionResult<bool> IsPartBoosted(int partId)
        => Ok(_service.IsPartBoosted(partId));
}
