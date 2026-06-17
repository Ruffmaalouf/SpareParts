using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpareParts.Domain.Qr;
using SpareParts.Infrastructure.Services;

namespace SpareParts.Api.Controllers;

[ApiController]
[Route("api/qr-tag")]
[Authorize]
public sealed class QrTagController : SparePartsControllerBase
{
    private readonly QrTagService _service;

    public QrTagController(QrTagService service)
    {
        _service = service;
    }

    [HttpGet("{partId:int}")]
    public ActionResult<QrTagDto> Generate(int partId)
        => Ok(_service.Generate(partId));
}
