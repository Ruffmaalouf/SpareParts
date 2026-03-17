using Microsoft.AspNetCore.Mvc;
using SpareParts.Domain.Purchases;
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
            var userId = GetUserId();
            var result = _purchaseService.CreatePurchase(request, userId);
            return Ok(result);
        }

        private int GetUserId()
        {
            var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            return claim != null ? int.Parse(claim.Value) : 1;
        }
    }
}
