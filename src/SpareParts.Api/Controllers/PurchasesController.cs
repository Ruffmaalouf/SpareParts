using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SpareParts.Domain.Purchases;
using SpareParts.Infrastructure.Services;

namespace SpareParts.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
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
            return Ok(result);
        }

        [HttpGet("used-cars")]
        public ActionResult<IReadOnlyList<UsedCarPurchaseSummaryDto>> GetUsedCarPurchases()
            => Ok(_purchaseService.GetUsedCarPurchases());

        [HttpPost("used-cars")]
        public ActionResult<CreateUsedCarPurchaseResponse> CreateUsedCarPurchase([FromBody] CreateUsedCarPurchaseRequest request)
        {
            var result = _purchaseService.CreateUsedCarPurchase(request, CurrentUserId);
            return Ok(result);
        }
    }
}
