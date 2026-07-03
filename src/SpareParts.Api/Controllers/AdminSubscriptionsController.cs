using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SpareParts.Api.Infrastructure;
using SpareParts.Domain.Pricing;

namespace SpareParts.Api.Controllers;

[ApiController]
[Route("api/admin/subscriptions")]
[Authorize(Policy = AuthorizationPolicies.SuperAdmin)]
public sealed class AdminSubscriptionsController : SparePartsControllerBase
{
    private readonly ISubscriptionService _service;
    private readonly ILogger<AdminSubscriptionsController> _logger;

    public AdminSubscriptionsController(ISubscriptionService service, ILogger<AdminSubscriptionsController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet]
    public ActionResult<IReadOnlyList<TenantSubscriptionDto>> GetAll()
        => Ok(_service.GetAllForAdmin());

    [HttpPost("activate")]
    public ActionResult<TenantSubscriptionDto> Activate([FromBody] AdminActivateSubscriptionRequest request)
    {
        var result = _service.AdminActivate(request, CurrentUserId);

        _logger.LogInformation(
            "Admin activated subscription for TenantId={TenantId} Package={PackageCode} BillingCycle={BillingCycle} ByUserId={UserId} TraceId={TraceId}",
            request.TenantId, request.PackageCode, request.BillingCycle, CurrentUserId, HttpContext.TraceIdentifier);

        return Ok(result);
    }

    [HttpPost("run-maintenance")]
    public ActionResult<int> RunMaintenance()
    {
        var affected = _service.RunMaintenance();

        _logger.LogInformation(
            "Admin ran subscription maintenance. AffectedCount={AffectedCount} ByUserId={UserId} TraceId={TraceId}",
            affected, CurrentUserId, HttpContext.TraceIdentifier);

        return Ok(affected);
    }
}
