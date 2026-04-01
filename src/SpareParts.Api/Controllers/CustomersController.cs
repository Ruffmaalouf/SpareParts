using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpareParts.Domain.BusinessPartners;
using SpareParts.Infrastructure.Services;

namespace SpareParts.Api.Controllers
{
    [ApiController]
    [Route("api/customers")]
    [Authorize]
    public class CustomersController : ControllerBase
    {
        private readonly CustomersService _service;

        public CustomersController(CustomersService service)
        {
            _service = service;
        }

        [HttpGet]
        public ActionResult<IEnumerable<CustomerDto>> GetAll([FromQuery] string? search = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 100)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 500);

            var result = _service.GetAll(search, page, pageSize);
            Response.Headers["X-Page"] = page.ToString();
            Response.Headers["X-Page-Size"] = pageSize.ToString();
            Response.Headers["X-Total-Count"] = result.TotalCount.ToString();
            return Ok(result.Items);
        }

        [HttpPost]
        public ActionResult<int> Create([FromBody] CreateCustomerRequest req)
            => Ok(_service.Create(req, GetUserId()));

        [HttpPut("{id:int}")]
        public IActionResult Update(int id, [FromBody] CreateCustomerRequest req)
        {
            _service.Update(id, req, GetUserId());
            return NoContent();
        }

        [HttpDelete("{id:int}")]
        public IActionResult Delete(int id)
        {
            _service.Delete(id);
            return NoContent();
        }

        private int GetUserId()
        {
            var c = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (c == null || !int.TryParse(c.Value, out var userId))
            {
                throw new UnauthorizedAccessException("User identifier claim is missing.");
            }

            return userId;
        }
    }
}
