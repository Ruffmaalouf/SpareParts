using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpareParts.Api.Infrastructure;
using SpareParts.Domain.Pricing;

namespace SpareParts.Api.Controllers;

[ApiController]
[Route("api/admin/pricing/packages")]
[Authorize(Policy = AuthorizationPolicies.SuperAdmin)]
public sealed class AdminPricingController : SparePartsControllerBase
{
    private readonly IPricingPackageService _service;

    public AdminPricingController(IPricingPackageService service)
    {
        _service = service;
    }

    [HttpGet]
    public ActionResult<IReadOnlyList<PricingPackageDto>> GetAll()
        => Ok(_service.GetAll(activeOnly: false));

    [HttpGet("{id:int}")]
    public ActionResult<PricingPackageDto> GetById(int id)
        => Ok(_service.GetById(id));

    [HttpPost]
    public ActionResult<int> Create([FromBody] UpsertPricingPackageRequest request)
        => Ok(_service.Create(request, CurrentUserId));

    [HttpPut("{id:int}")]
    public IActionResult Update(int id, [FromBody] UpsertPricingPackageRequest request)
    {
        _service.Update(id, request, CurrentUserId);
        return NoContent();
    }

    [HttpPut("{id:int}/active")]
    public IActionResult SetActive(int id, [FromBody] SetPackageActiveRequest request)
    {
        _service.SetActive(id, request.IsActive, CurrentUserId);
        return NoContent();
    }
}
