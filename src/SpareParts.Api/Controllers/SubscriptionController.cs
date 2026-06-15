using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpareParts.Api.Infrastructure;
using SpareParts.Domain.Pricing;

namespace SpareParts.Api.Controllers;

[ApiController]
[Route("api/subscription")]
[Authorize]
public sealed class SubscriptionController : SparePartsControllerBase
{
    private readonly ISubscriptionService _service;

    public SubscriptionController(ISubscriptionService service)
    {
        _service = service;
    }

    [HttpGet("current")]
    public ActionResult<TenantSubscriptionDto> GetCurrent()
        => Ok(_service.GetCurrent(CurrentTenantId));

    [HttpPost("start-free")]
    [Authorize(Policy = AuthorizationPolicies.AdminOrManager)]
    public ActionResult<TenantSubscriptionDto> StartFree()
        => Ok(_service.StartFree(CurrentTenantId, CurrentUserId));

    [HttpPost("start-trial")]
    [Authorize(Policy = AuthorizationPolicies.AdminOrManager)]
    public ActionResult<TenantSubscriptionDto> StartTrial([FromBody] StartTrialRequest request)
        => Ok(_service.StartTrial(CurrentTenantId, request, CurrentUserId));

    [HttpPost("checkout")]
    [Authorize(Policy = AuthorizationPolicies.AdminOrManager)]
    public async Task<ActionResult<CheckoutResponseDto>> Checkout(
        [FromBody] CheckoutRequest request,
        CancellationToken cancellationToken)
        => Ok(await _service.CheckoutAsync(CurrentTenantId, request, CurrentUserId, cancellationToken));

    [HttpPost("change-plan")]
    [Authorize(Policy = AuthorizationPolicies.AdminOrManager)]
    public ActionResult<TenantSubscriptionDto> ChangePlan([FromBody] ChangePlanRequest request)
        => Ok(_service.ChangePlan(CurrentTenantId, request, CurrentUserId));

    [HttpPost("cancel")]
    [Authorize(Policy = AuthorizationPolicies.AdminOrManager)]
    public IActionResult Cancel([FromBody] CancelSubscriptionRequest request)
    {
        _service.Cancel(CurrentTenantId, request, CurrentUserId);
        return NoContent();
    }
}
