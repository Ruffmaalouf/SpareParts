using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpareParts.Domain.InstantOffer;
using SpareParts.Infrastructure.Services;

namespace SpareParts.Api.Controllers;

[ApiController]
[Route("api/instant-offers")]
[Authorize]
public sealed class InstantOfferController : SparePartsControllerBase
{
    private readonly InstantOfferService _service;

    public InstantOfferController(InstantOfferService service)
    {
        _service = service;
    }

    [HttpGet]
    public IActionResult GetAll([FromQuery] InstantOfferStatus? status)
        => Ok(_service.GetAll(status));

    [HttpGet("{id:int}")]
    public IActionResult GetById(int id)
    {
        var dto = _service.GetById(id);
        if (dto is null) return NotFound();
        return Ok(dto);
    }

    [HttpPost]
    public ActionResult<InstantOfferDto> Submit([FromBody] SubmitInstantOfferRequest request)
        => Ok(_service.Submit(request));

    [HttpPatch("{id:int}/status")]
    public IActionResult UpdateStatus(int id, [FromBody] UpdateInstantOfferStatusRequest request)
    {
        _service.UpdateStatus(id, request);
        return NoContent();
    }
}
