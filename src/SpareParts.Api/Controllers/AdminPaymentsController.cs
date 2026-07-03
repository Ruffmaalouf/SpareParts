using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SpareParts.Api.Infrastructure;
using SpareParts.Domain.Pricing;

namespace SpareParts.Api.Controllers;

[ApiController]
[Route("api/admin/payments")]
[Authorize(Policy = AuthorizationPolicies.SuperAdmin)]
public sealed class AdminPaymentsController : SparePartsControllerBase
{
    private readonly IPaymentService _service;
    private readonly ILogger<AdminPaymentsController> _logger;

    public AdminPaymentsController(IPaymentService service, ILogger<AdminPaymentsController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet]
    public ActionResult<IReadOnlyList<PaymentDto>> GetAll()
        => Ok(_service.GetAllForAdmin());

    [HttpPost("{id:int}/mark-paid")]
    public ActionResult<PaymentDto> MarkPaid(int id, [FromBody] MarkPaymentPaidRequest request)
    {
        var result = _service.MarkPaid(id, request, CurrentUserId);

        _logger.LogInformation(
            "Admin marked payment {PaymentId} as paid. TenantId={TenantId} ByUserId={UserId} TraceId={TraceId}",
            id, result.TenantId, CurrentUserId, HttpContext.TraceIdentifier);

        return Ok(result);
    }

    [HttpGet("/api/admin/webhook-events")]
    public ActionResult<IReadOnlyList<WebhookEventDto>> GetWebhookEvents()
        => Ok(_service.GetWebhookEventsForAdmin());
}
