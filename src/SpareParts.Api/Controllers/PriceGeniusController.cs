using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpareParts.Domain.Pricing;
using SpareParts.Infrastructure.Services;

namespace SpareParts.Api.Controllers;

[ApiController]
[Route("api/price-genius")]
[Authorize]
public sealed class PriceGeniusController : SparePartsControllerBase
{
    private readonly PriceGeniusService _service;

    public PriceGeniusController(PriceGeniusService service)
    {
        _service = service;
    }

    [HttpPost("suggest")]
    public ActionResult<PriceGeniusSuggestion> Suggest([FromBody] PriceGeniusRequest request)
        => Ok(_service.Suggest(request));
}
