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

        private int GetUserId()
        {
            var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (claim == null || !int.TryParse(claim.Value, out var userId))
                throw new UnauthorizedAccessException("User identifier claim is missing.");
            return userId;
        }
    }
}
