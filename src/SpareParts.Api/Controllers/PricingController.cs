using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpareParts.Domain.Pricing;

namespace SpareParts.Api.Controllers;

[ApiController]
[Route("api/pricing")]
[AllowAnonymous]
public sealed class PricingController : SparePartsControllerBase
{
    private readonly IPricingPackageService _service;

    public PricingController(IPricingPackageService service)
    {
        _service = service;
    }

    [HttpGet("packages")]
    public ActionResult<IReadOnlyList<PricingPackageDto>> GetPackages()
        => Ok(_service.GetAll(activeOnly: true));

    [HttpGet("packages/{code}")]
    public ActionResult<PricingPackageDto> GetPackage(string code)
        => Ok(_service.GetByCode(code));
}
