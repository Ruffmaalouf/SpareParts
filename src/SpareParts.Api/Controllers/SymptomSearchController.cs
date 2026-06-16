using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpareParts.Domain.SymptomSearch;
using SpareParts.Infrastructure.Services;

namespace SpareParts.Api.Controllers;

[ApiController]
[Route("api/symptom-search")]
[Authorize]
public sealed class SymptomSearchController : SparePartsControllerBase
{
    private readonly SymptomSearchService _service;

    public SymptomSearchController(SymptomSearchService service)
    {
        _service = service;
    }

    [HttpPost]
    public ActionResult<SymptomSearchResponse> Search([FromBody] SymptomSearchRequest request)
        => Ok(_service.Diagnose(request));
}
