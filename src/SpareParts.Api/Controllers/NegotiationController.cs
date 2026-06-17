using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpareParts.Domain.Negotiation;
using SpareParts.Infrastructure.Services;

namespace SpareParts.Api.Controllers;

[ApiController]
[Route("api/negotiations")]
[Authorize]
public sealed class NegotiationController : SparePartsControllerBase
{
    private readonly NegotiationBotService _service;

    public NegotiationController(NegotiationBotService service)
    {
        _service = service;
    }

    [HttpGet]
    public IActionResult GetAll([FromQuery] NegotiationStatus? status)
        => Ok(_service.GetAll(status));

    [HttpGet("{id:int}")]
    public IActionResult GetById(int id)
    {
        var dto = _service.GetById(id);
        if (dto is null) return NotFound();
        return Ok(dto);
    }

    [HttpPost]
    public ActionResult<NegotiationSessionDto> Start([FromBody] CreateNegotiationRequest request)
        => Ok(_service.Start(request, CurrentUserId));

    [HttpPost("{id:int}/offer")]
    public ActionResult<NegotiationSessionDto> MakeOffer(int id, [FromBody] MakeNegotiationOfferRequest request)
        => Ok(_service.MakeOffer(id, request));

    [HttpPost("{id:int}/accept")]
    public IActionResult Accept(int id)
    {
        _service.Accept(id);
        return NoContent();
    }

    [HttpPost("{id:int}/reject")]
    public IActionResult Reject(int id)
    {
        _service.Reject(id);
        return NoContent();
    }
}
