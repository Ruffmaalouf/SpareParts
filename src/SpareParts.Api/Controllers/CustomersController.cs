using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpareParts.Domain.BusinessPartners;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Api.Controllers
{
    [ApiController]
    [Route("api/customers")]
    [Authorize]
    public class CustomersController : ControllerBase
    {
        private readonly ISqlConnectionFactory _factory;
        public CustomersController(ISqlConnectionFactory factory) => _factory = factory;

        [HttpGet]
        public ActionResult<IEnumerable<CustomerDto>> GetAll([FromQuery] string? search = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 100)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 500);

            using var session = new DbSession(_factory);
            var customersRepository = new CustomersRepository(session);
            var all = customersRepository.GetAll();
            if (!string.IsNullOrWhiteSpace(search))
            {
                all = all.Where(c => c.Name.Contains(search, StringComparison.OrdinalIgnoreCase));
            }

            var projected = all.Select(c => new CustomerDto
            {
                Id = c.Id,
                Name = c.Name,
                Phone = c.Phone,
                Email = c.Email,
                Address = c.Address,
                TaxNumber = c.TaxNumber,
                OpeningBalance = c.OpeningBalance
            });

            Response.Headers["X-Page"] = page.ToString();
            Response.Headers["X-Page-Size"] = pageSize.ToString();
            Response.Headers["X-Total-Count"] = projected.Count().ToString();
            return Ok(projected.Skip((page - 1) * pageSize).Take(pageSize));
        }

        [HttpPost]
        public ActionResult<int> Create([FromBody] CreateCustomerRequest req)
        {
            using var session = new DbSession(_factory);
            var customersRepository = new CustomersRepository(session);
            var customer = new Customer
            {
                Name = req.Name,
                Phone = req.Phone,
                Email = req.Email,
                Address = req.Address,
                TaxNumber = req.TaxNumber,
                OpeningBalance = req.OpeningBalance,
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = GetUserId()
            };
            var id = customersRepository.Insert(customer);
            session.Commit();
            return Ok(id);
        }

        [HttpPut("{id:int}")]
        public IActionResult Update(int id, [FromBody] CreateCustomerRequest req)
        {
            using var session = new DbSession(_factory);
            var customersRepository = new CustomersRepository(session);
            if (!customersRepository.Update(id, req, GetUserId())) return NotFound();
            session.Commit();
            return NoContent();
        }

        [HttpDelete("{id:int}")]
        public IActionResult Delete(int id)
        {
            using var session = new DbSession(_factory);
            var customersRepository = new CustomersRepository(session);
            if (!customersRepository.Delete(id)) return NotFound();
            session.Commit();
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
