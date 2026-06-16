using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpareParts.Domain.Sales;
using SpareParts.Infrastructure.Services;

namespace SpareParts.Api.Controllers
{
    [ApiController]
    [Route("api/sales-returns")]
    [Authorize]
    public class SalesReturnsController : SparePartsControllerBase
    {
        private readonly SalesReturnsService _service;

        public SalesReturnsController(SalesReturnsService service)
        {
            _service = service;
        }

        [HttpPost]
        public ActionResult<CreateSalesReturnResponse> CreateReturn([FromBody] CreateSalesReturnRequest request)
        {
            var result = _service.CreateReturn(request, CurrentUserId);
            return CreatedAtAction(nameof(GetById), new { returnId = result.ReturnId }, result);
        }

        [HttpGet]
        public ActionResult<List<SalesReturnLookupDto>> Search([FromQuery] string? search = null)
            => Ok(_service.SearchReturns(search));

        [HttpGet("{returnId:int}")]
        public ActionResult<SalesReturnDetailsDto> GetById(int returnId)
            => Ok(_service.GetReturnById(returnId)
                ?? throw new NotFoundException($"Sales return {returnId} not found."));

        [HttpGet("returnable/{invoiceId:int}")]
        public ActionResult<List<ReturnableLineDto>> GetReturnableLines(int invoiceId)
            => Ok(_service.GetReturnableLines(invoiceId));
    }
}
