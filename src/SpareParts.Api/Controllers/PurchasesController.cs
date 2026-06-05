using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SpareParts.Domain.Purchases;
using SpareParts.Infrastructure.Services;

namespace SpareParts.Api.Controllers
{
    [ApiController]
    [Route("api/purchases")]
    [Authorize]
    public class PurchasesController : SparePartsControllerBase
    {
        private readonly PurchaseService _purchaseService;

        public PurchasesController(PurchaseService purchaseService)
        {
            _purchaseService = purchaseService;
        }

        [HttpPost]
        public ActionResult<CreatePurchaseResponse> CreatePurchase([FromBody] CreatePurchaseRequest request)
        {
            var result = _purchaseService.CreatePurchase(request, CurrentUserId);
            return CreatedAtAction(nameof(GetById), new { purchaseId = result.PurchaseId }, result);
        }

        [HttpGet]
        public ActionResult<List<PurchaseInvoiceLookupDto>> SearchPurchases([FromQuery] string? search = null)
        {
            var purchases = _purchaseService.SearchPurchases(search);
            return Ok(purchases);
        }

        [HttpGet("{purchaseId:int}")]
        public ActionResult<PurchaseInvoiceDetailsDto> GetById(int purchaseId)
            => Ok(_purchaseService.GetPurchaseById(purchaseId)
                ?? throw new NotFoundException($"Purchase invoice {purchaseId} not found."));

        [HttpPut("{purchaseId:int}")]
        public IActionResult UpdatePurchase(int purchaseId, [FromBody] UpdatePurchaseRequest request)
        {
            if (!_purchaseService.UpdatePurchase(purchaseId, request, CurrentUserId))
            {
                throw new NotFoundException($"Purchase invoice {purchaseId} not found.");
            }

            return NoContent();
        }

        [HttpGet("used-cars")]
        public ActionResult<IReadOnlyList<UsedCarPurchaseSummaryDto>> GetUsedCarPurchases()
            => Ok(_purchaseService.GetUsedCarPurchases());

        [HttpGet("used-cars/{id:int}")]
        public ActionResult<UsedCarPurchaseDetailDto> GetUsedCarPurchase(int id)
            => Ok(_purchaseService.GetUsedCarPurchase(id));

        [HttpPost("used-cars")]
        public ActionResult<CreateUsedCarPurchaseResponse> CreateUsedCarPurchase([FromBody] CreateUsedCarPurchaseRequest request)
        {
            var result = _purchaseService.CreateUsedCarPurchase(request, CurrentUserId);
            return CreatedAtAction(nameof(GetUsedCarPurchase), new { id = result.PurchaseId }, result);
        }

        [HttpPost("used-cars/{id:int}/post")]
        public ActionResult<PostUsedCarPurchaseResponse> PostUsedCarPurchase(int id)
        {
            var result = _purchaseService.PostUsedCarPurchase(id, CurrentUserId);
            return Ok(result);
        }

        [HttpDelete("used-cars/{id:int}")]
        public IActionResult DeleteUsedCarPurchase(int id)
        {
            _purchaseService.DeleteUsedCarPurchase(id);
            return NoContent();
        }
    }
}
