using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpareParts.Domain.Insurance;
using SpareParts.Infrastructure.Services;

namespace SpareParts.Api.Controllers;

[ApiController]
[Route("api/part-insurance")]
[Authorize]
public sealed class PartInsuranceController : SparePartsControllerBase
{
    private readonly PartInsuranceService _service;

    public PartInsuranceController(PartInsuranceService service)
    {
        _service = service;
    }

    [HttpGet("options")]
    public IActionResult GetOptions()
        => Ok(_service.GetOptions());

    [HttpPost("addon")]
    public ActionResult<InsuranceAddonDto> AddAddon([FromBody] AddInsuranceAddonRequest request)
        => Ok(_service.AddAddon(request, CurrentUserId));
}
