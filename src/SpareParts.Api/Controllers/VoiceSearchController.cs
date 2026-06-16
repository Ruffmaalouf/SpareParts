using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpareParts.Domain.Marketplace;
using SpareParts.Domain.Search;
using SpareParts.Infrastructure.Services;

namespace SpareParts.Api.Controllers;

[ApiController]
[Route("api/voice-search")]
[Authorize]
public sealed class VoiceSearchController : SparePartsControllerBase
{
    private readonly SmartSearchService _smartSearchService;

    public VoiceSearchController(SmartSearchService smartSearchService)
    {
        _smartSearchService = smartSearchService;
    }

    [HttpPost]
    public ActionResult<SmartSearchResponseDto> Search([FromBody] VoiceSearchRequest request)
        => Ok(_smartSearchService.Search(request.Transcript));
}
