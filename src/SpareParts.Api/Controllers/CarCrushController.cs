using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpareParts.Domain.Marketplace;
using SpareParts.Infrastructure.Services;

namespace SpareParts.Api.Controllers;

[ApiController]
[Route("api/car-crush")]
[Authorize]
public sealed class CarCrushController : SparePartsControllerBase
{
    private readonly CarCrushService _service;

    public CarCrushController(CarCrushService service)
    {
        _service = service;
    }

    [HttpPost("generate")]
    public ActionResult<CarCrushResult> Generate([FromBody] CarCrushRequest request)
        => Ok(_service.Generate(request));

    [HttpPost("create-listings")]
    public IActionResult CreateListings([FromBody] CreateDraftListingsRequest body)
    {
        _service.CreateDraftListings(
            body.Vehicle ?? new CarCrushRequest(),
            body.SelectedPartNames,
            CurrentUserId);
        return NoContent();
    }
}
