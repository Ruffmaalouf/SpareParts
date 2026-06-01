using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpareParts.Domain.Growth;
using SpareParts.Infrastructure.Services;

namespace SpareParts.Api.Controllers;

[ApiController]
[Route("api/growth")]
[Authorize]
public sealed class GrowthController : SparePartsControllerBase
{
    private readonly GrowthIntelligenceService _service;

    public GrowthController(GrowthIntelligenceService service)
    {
        _service = service;
    }

    [HttpGet("briefing")]
    public ActionResult<GrowthBriefingDto> GetBriefing()
        => Ok(_service.GetBriefing());

    [HttpPost("auction-simulator")]
    public ActionResult<AuctionProfitSimulationDto> SimulateAuction(
        [FromBody] AuctionProfitSimulationRequest request)
        => Ok(_service.SimulateAuction(request));

    [HttpPost("voice-quote")]
    public ActionResult<VoiceQuoteDto> BuildVoiceQuote([FromBody] VoiceQuoteRequest request)
        => Ok(_service.BuildVoiceQuote(request));
}
