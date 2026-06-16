using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpareParts.Domain.Reputation;
using SpareParts.Infrastructure.Services;

namespace SpareParts.Api.Controllers;

[ApiController]
[Route("api/seller-reputation")]
[Authorize]
public sealed class SellerReputationController : SparePartsControllerBase
{
    private readonly SellerReputationService _service;

    public SellerReputationController(SellerReputationService service)
    {
        _service = service;
    }

    [HttpGet]
    public ActionResult<SellerReputationDto> GetMine()
    {
        var result = _service.GetReputation(CurrentTenantId);
        if (result is null) return NotFound();
        return Ok(result);
    }

    [HttpGet("{tenantId:int}")]
    public ActionResult<SellerReputationDto> GetByTenant(int tenantId)
    {
        var result = _service.GetReputation(tenantId);
        if (result is null) return NotFound();
        return Ok(result);
    }

    [HttpGet("all")]
    public ActionResult<IEnumerable<SellerReputationDto>> GetAll()
        => Ok(_service.GetAll());
}
