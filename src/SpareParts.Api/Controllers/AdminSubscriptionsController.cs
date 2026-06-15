using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpareParts.Api.Infrastructure;
using SpareParts.Domain.Pricing;

namespace SpareParts.Api.Controllers;

[ApiController]
[Route("api/admin/subscriptions")]
[Authorize(Policy = AuthorizationPolicies.SuperAdmin)]
public sealed class AdminSubscriptionsController : SparePartsControllerBase
{
    private readonly ISubscriptionService _service;

    public AdminSubscriptionsController(ISubscriptionService service)
    {
        _service = service;
    }

    [HttpGet]
    public ActionResult<IReadOnlyList<TenantSubscriptionDto>> GetAll()
        => Ok(_service.GetAllForAdmin());

    [HttpPost("activate")]
    public ActionResult<TenantSubscriptionDto> Activate([FromBody] AdminActivateSubscriptionRequest request)
        => Ok(_service.AdminActivate(request, CurrentUserId));

    [HttpPost("run-maintenance")]
    public ActionResult<int> RunMaintenance()
        => Ok(_service.RunMaintenance());
}
