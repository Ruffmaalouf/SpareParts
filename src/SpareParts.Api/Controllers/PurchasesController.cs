using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SpareParts.Domain.Purchases;
using SpareParts.Infrastructure.Services;

namespace SpareParts.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
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
            if (claim == null || !int.TryParse(claim.Value, out var userId))
                throw new UnauthorizedAccessException("User identifier claim is missing.");
            return userId;
        }
    }
}
