using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpareParts.Domain.Inventory;
using SpareParts.Infrastructure.Services;

namespace SpareParts.Api.Controllers;

[ApiController]
[Route("api/parts/{partId:int}/substitutes")]
[Authorize]
public sealed class PartSubstitutesController : SparePartsControllerBase
{
    private readonly PartSubstitutesService _service;

    public PartSubstitutesController(PartSubstitutesService service)
    {
        _service = service;
    }

    [HttpGet]
    public ActionResult<IReadOnlyList<PartSubstituteDto>> GetSubstitutes(int partId)
        => Ok(_service.GetSubstitutes(partId));

    [HttpPost]
    public IActionResult AddSubstitute(int partId, [FromBody] AddPartSubstituteRequest req)
    {
        _service.AddSubstitute(partId, req, CurrentUserId);
        return NoContent();
    }

    [HttpDelete("{substituteId:int}")]
    public IActionResult RemoveSubstitute(int partId, int substituteId)
    {
        _service.RemoveSubstitute(substituteId);
        return NoContent();
    }
}
