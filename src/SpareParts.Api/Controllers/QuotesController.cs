using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpareParts.Api.Infrastructure;
using SpareParts.Domain.Sales;
using SpareParts.Infrastructure.Services;

namespace SpareParts.Api.Controllers
{
    [ApiController]
    [Route("api/quotes")]
    [Authorize]
    public sealed class QuotesController : SparePartsControllerBase
    {
        private readonly QuotesService _service;

        public QuotesController(QuotesService service)
        {
            _service = service;
        }

        [HttpGet]
        public ActionResult<IEnumerable<QuoteLookupDto>> GetAll(
            [FromQuery] string? status = null,
            [FromQuery] string? search = null)
            => Ok(_service.GetAll(status, search));

        [HttpGet("{id:int}")]
        public ActionResult<QuoteDetailsDto> GetById(int id)
        {
            var quote = _service.GetById(id);
            return quote == null ? NotFound() : Ok(quote);
        }

        [HttpPost]
        public ActionResult<int> Create([FromBody] CreateQuoteRequest request)
        {
            var id = _service.Create(request, CurrentUserId);
            return CreatedAtAction(nameof(GetById), new { id }, id);
        }

        [HttpPut("{id:int}/status")]
        public IActionResult UpdateStatus(int id, [FromBody] UpdateQuoteStatusRequest request)
        {
            _service.UpdateStatus(id, request.Status);
            return NoContent();
        }

        [HttpPost("{id:int}/convert")]
        public ActionResult<ConvertQuoteToInvoiceResponse> ConvertToInvoice(int id)
        {
            var result = _service.ConvertToInvoice(id, CurrentUserId);
            return Ok(result);
        }

        [HttpDelete("{id:int}")]
        [Authorize(Policy = AuthorizationPolicies.AdminOrManager)]
        public IActionResult Delete(int id)
        {
            _service.Delete(id);
            return NoContent();
        }
    }
}
