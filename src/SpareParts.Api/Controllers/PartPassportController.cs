using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpareParts.Domain.Passport;
using SpareParts.Infrastructure.Services;

namespace SpareParts.Api.Controllers;

[ApiController]
[Route("api/part-passport")]
[Authorize]
public sealed class PartPassportController : SparePartsControllerBase
{
    private readonly PartPassportService _service;

    public PartPassportController(PartPassportService service)
    {
        _service = service;
    }

    [HttpGet("{partId:int}")]
    public ActionResult<PartPassportDto> GetPassport(int partId)
    {
        var dto = _service.GetPassport(partId);
        if (dto is null) return NotFound();
        return Ok(dto);
    }

    [HttpPost("photo")]
    public ActionResult<PartPassportPhotoDto> AddPhoto([FromBody] AddPassportPhotoRequest request)
        => Ok(_service.AddPhoto(request, CurrentUserId));
}
