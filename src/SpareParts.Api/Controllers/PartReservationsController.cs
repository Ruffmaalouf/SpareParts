using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpareParts.Domain.Marketplace;
using SpareParts.Infrastructure.Services;

namespace SpareParts.Api.Controllers;

[ApiController]
[Route("api/part-reservations")]
[Authorize]
public sealed class PartReservationsController : SparePartsControllerBase
{
    private readonly PartReservationService _service;

    public PartReservationsController(PartReservationService service)
    {
        _service = service;
    }

    [HttpGet]
    public ActionResult<IEnumerable<PartReservationDto>> GetAll([FromQuery] PartReservationStatus? status = null)
        => Ok(_service.GetAllForTenant(status));

    [HttpPost]
    public ActionResult<int> Create([FromBody] CreatePartReservationRequest request)
        => Ok(_service.Create(request, CurrentUserId));

    [HttpPut("{id:int}/release")]
    public IActionResult Release(int id)
    {
        _service.Release(id, CurrentUserId);
        return NoContent();
    }
}
