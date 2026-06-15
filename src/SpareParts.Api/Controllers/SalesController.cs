using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpareParts.Domain.Sales;
using SpareParts.Infrastructure.Services;

namespace SpareParts.Api.Controllers
{
    [ApiController]
    [Route("api/sales")]
    [Authorize]
    public class SalesController : SparePartsControllerBase
    {
        private readonly SalesService _salesService;

        public SalesController(SalesService salesService)
        {
            _salesService = salesService;
        }

        [HttpPost]
        public ActionResult<CreateSaleResponse> CreateSale([FromBody] CreateSaleRequest request)
        {
            var result = _salesService.CreateSale(request, CurrentUserId);
            return CreatedAtAction(nameof(GetById), new { invoiceId = result.InvoiceId }, result);
        }

        [HttpGet]
        public ActionResult<List<SalesInvoiceLookupDto>> SearchInvoices([FromQuery] string? search = null)
        {
            var invoices = _salesService.SearchInvoices(search);
            return Ok(invoices);
        }

        [HttpGet("{invoiceId:int}")]
        public ActionResult<SalesInvoiceDetailsDto> GetById(int invoiceId)
            => Ok(_salesService.GetInvoiceById(invoiceId)
                ?? throw new NotFoundException($"Sale invoice {invoiceId} not found."));

        [HttpGet("profit-history")]
        public ActionResult<List<SalesProfitHistoryPointDto>> GetProfitHistory([FromQuery] int months = 12)
            => Ok(_salesService.GetProfitHistory(months));

        [HttpPut("{invoiceId:int}")]
        public IActionResult UpdateSale(int invoiceId, [FromBody] UpdateSaleRequest request)
        {
            if (!_salesService.UpdateInvoice(invoiceId, request, CurrentUserId))
            {
                throw new NotFoundException($"Sale invoice {invoiceId} not found.");
            }

            return NoContent();
        }

        [HttpPost("{invoiceId:int}/payments")]
        public ActionResult<IssueSalePaymentResponse> IssuePayment(int invoiceId, [FromBody] IssueSalePaymentRequest request)
        {
            var result = _salesService.IssuePayment(invoiceId, request, CurrentUserId);
            return Ok(result);
        }
    }
}
