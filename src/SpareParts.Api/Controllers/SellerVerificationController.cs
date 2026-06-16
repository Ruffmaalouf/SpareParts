using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpareParts.Api.Infrastructure;
using SpareParts.Domain.Verification;
using SpareParts.Infrastructure.Services;

namespace SpareParts.Api.Controllers;

[ApiController]
[Route("api/seller-verification")]
[Authorize]
public sealed class SellerVerificationController : SparePartsControllerBase
{
    private readonly SellerVerificationService _service;

    public SellerVerificationController(SellerVerificationService service)
    {
        _service = service;
    }

    [HttpGet]
    public ActionResult<SellerVerificationDto> GetMine()
        => Ok(_service.Get(CurrentTenantId));

    [HttpPost("submit")]
    public IActionResult Submit([FromBody] UpdateSellerVerificationRequest request)
    {
        _service.Submit(CurrentTenantId, request, CurrentUserId);
        return NoContent();
    }

    [HttpPut("admin/decide")]
    [Authorize(Policy = AuthorizationPolicies.Admin)]
    public IActionResult AdminDecide([FromBody] AdminVerificationDecisionRequest request)
    {
        _service.AdminDecide(request, CurrentUserId);
        return NoContent();
    }

    [HttpGet("admin/pending")]
    [Authorize(Policy = AuthorizationPolicies.Admin)]
    public ActionResult<IEnumerable<SellerVerificationDto>> GetPending()
        => Ok(_service.GetPending());
}
