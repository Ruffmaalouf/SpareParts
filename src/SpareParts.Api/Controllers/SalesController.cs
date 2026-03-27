using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SpareParts.Domain.Sales;
using SpareParts.Infrastructure.Services;

namespace SpareParts.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SalesController : ControllerBase
    {
        private readonly SalesService _salesService;

        public SalesController(SalesService salesService)
        {
            _salesService = salesService;
        }

        [HttpPost]
        public ActionResult<CreateSaleResponse> CreateSale([FromBody] CreateSaleRequest request)
        {
            var userId = GetUserId();
            var result = _salesService.CreateSale(request, userId);
            return Ok(result);
        }

        [HttpGet]
        public ActionResult<List<SalesInvoiceLookupDto>> SearchInvoices([FromQuery] string? search = null)
        {
            var invoices = _salesService.SearchInvoices(search);
            return Ok(invoices);
        }

        [HttpGet("{invoiceId:int}")]
        public ActionResult<SalesInvoiceDetailsDto> GetById(int invoiceId)
        {
            var invoice = _salesService.GetInvoiceById(invoiceId);
            if (invoice == null)
            {
                return NotFound();
            }

            return Ok(invoice);
        }

        [HttpPut("{invoiceId:int}")]
        public IActionResult UpdateSale(int invoiceId, [FromBody] UpdateSaleRequest request)
        {
            var userId = GetUserId();
            var updated = _salesService.UpdateInvoice(invoiceId, request, userId);
            if (!updated)
            {
                return NotFound();
            }

            return NoContent();
        }

        private int GetUserId()
        {
            var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (claim == null || !int.TryParse(claim.Value, out var userId))
                throw new UnauthorizedAccessException("User identifier claim is missing.");
            return userId;
        }
    }
}
