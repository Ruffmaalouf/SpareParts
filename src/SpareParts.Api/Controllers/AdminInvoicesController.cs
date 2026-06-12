using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpareParts.Api.Infrastructure;
using SpareParts.Domain.Pricing;

namespace SpareParts.Api.Controllers;

[ApiController]
[Route("api/admin/invoices")]
[Authorize(Policy = AuthorizationPolicies.SuperAdmin)]
public sealed class AdminInvoicesController : SparePartsControllerBase
{
    private readonly IInvoiceService _service;

    public AdminInvoicesController(IInvoiceService service)
    {
        _service = service;
    }

    [HttpGet]
    public ActionResult<IReadOnlyList<InvoiceDto>> GetAll()
        => Ok(_service.GetAllForAdmin());
}
