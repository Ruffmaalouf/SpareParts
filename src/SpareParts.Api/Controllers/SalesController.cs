using Microsoft.AspNetCore.Mvc;
using SpareParts.Infrastructure.Services;

namespace SpareParts.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
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
            // TODO: get real user id from auth
            var userId = 1;
            var result = _salesService.CreateSale(request, userId);
            return Ok(result);
        }
    }
}
