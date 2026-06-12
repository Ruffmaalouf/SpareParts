using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpareParts.Domain.Pricing;

namespace SpareParts.Api.Controllers;

[ApiController]
[Route("api/invoices")]
[Authorize]
public sealed class InvoicesController : SparePartsControllerBase
{
    private readonly IInvoiceService _service;

    public InvoicesController(IInvoiceService service)
    {
        _service = service;
    }

    [HttpGet]
    public ActionResult<IReadOnlyList<InvoiceDto>> GetAll()
        => Ok(_service.GetAll(CurrentTenantId));

    [HttpGet("{id:int}")]
    public ActionResult<InvoiceDto> GetById(int id)
        => Ok(_service.GetById(CurrentTenantId, id));
}
