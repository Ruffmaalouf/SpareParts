using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpareParts.Domain.Pricing;

namespace SpareParts.Api.Controllers;

[ApiController]
[Route("api/payments")]
[Authorize]
public sealed class PaymentsController : SparePartsControllerBase
{
    private readonly IPaymentService _service;

    public PaymentsController(IPaymentService service)
    {
        _service = service;
    }

    [HttpGet("history")]
    public ActionResult<IReadOnlyList<PaymentDto>> GetHistory()
        => Ok(_service.GetHistory(CurrentTenantId));

    [HttpGet("{id:int}")]
    public ActionResult<PaymentDto> GetById(int id)
        => Ok(_service.GetById(CurrentTenantId, id));

    [HttpPost("webhook/{provider}")]
    [AllowAnonymous]
    public async Task<IActionResult> Webhook(string provider, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(Request.Body, Encoding.UTF8);
        var rawBody = await reader.ReadToEndAsync(cancellationToken);

        var context = new WebhookRequestContext
        {
            RawBody = rawBody,
            Headers = Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString()),
            Query = Request.Query.ToDictionary(q => q.Key, q => q.Value.ToString())
        };

        var result = await _service.HandleWebhookAsync(provider, context, cancellationToken);
        return Ok(result);
    }
}
