using Microsoft.AspNetCore.Mvc;
using SpareParts.Infrastructure.Services;

namespace SpareParts.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PurchasesController : ControllerBase
    {
        private readonly PurchaseService _purchaseService;

        public PurchasesController(PurchaseService purchaseService)
        {
            _purchaseService = purchaseService;
        }

        [HttpPost]
        public ActionResult<CreatePurchaseResponse> CreatePurchase([FromBody] CreatePurchaseRequest request)
        {
            var userId = 1; // TODO: get from auth later
            var result = _purchaseService.CreatePurchase(request, userId);
            return Ok(result);
        }
    }
}
