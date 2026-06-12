using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpareParts.Api.Infrastructure;
using SpareParts.Domain.Pricing;

namespace SpareParts.Api.Controllers;

[ApiController]
[Route("api/admin/payments")]
[Authorize(Policy = AuthorizationPolicies.SuperAdmin)]
public sealed class AdminPaymentsController : SparePartsControllerBase
{
    private readonly IPaymentService _service;

    public AdminPaymentsController(IPaymentService service)
    {
        _service = service;
    }

    [HttpGet]
    public ActionResult<IReadOnlyList<PaymentDto>> GetAll()
        => Ok(_service.GetAllForAdmin());

    [HttpPost("{id:int}/mark-paid")]
    public ActionResult<PaymentDto> MarkPaid(int id, [FromBody] MarkPaymentPaidRequest request)
        => Ok(_service.MarkPaid(id, request, CurrentUserId));

    [HttpGet("/api/admin/webhook-events")]
    public ActionResult<IReadOnlyList<WebhookEventDto>> GetWebhookEvents()
        => Ok(_service.GetWebhookEventsForAdmin());
}
