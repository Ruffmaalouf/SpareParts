using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpareParts.Domain.Genealogy;
using SpareParts.Infrastructure.Services;

namespace SpareParts.Api.Controllers;

[ApiController]
[Route("api/part-genealogy")]
[Authorize]
public sealed class PartGenealogyController : SparePartsControllerBase
{
    private readonly PartGenealogyService _service;

    public PartGenealogyController(PartGenealogyService service)
    {
        _service = service;
    }

    [HttpGet("{partId:int}")]
    public ActionResult<PartGenealogyDto> GetGenealogy(int partId)
    {
        var dto = _service.GetGenealogy(partId);
        if (dto is null) return NotFound();
        return Ok(dto);
    }

    [HttpPost("event")]
    public ActionResult<PartGenealogyEventDto> AddEvent([FromBody] AddGenealogyEventRequest request)
        => Ok(_service.AddEvent(request, CurrentUserId));
}
