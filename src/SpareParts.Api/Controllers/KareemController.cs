using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpareParts.Domain.Concierge;
using SpareParts.Infrastructure.Services;

namespace SpareParts.Api.Controllers;

[ApiController]
[Route("api/kareem")]
[Authorize]
public sealed class KareemController : SparePartsControllerBase
{
    private readonly KareemConciergeService _service;

    public KareemController(KareemConciergeService service)
    {
        _service = service;
    }

    [HttpPost("chat")]
    public ActionResult<KareemChatResponse> Chat([FromBody] KareemChatRequest request)
        => Ok(_service.Chat(request));
}
