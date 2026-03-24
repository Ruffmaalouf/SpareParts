using Dapper;
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
        public ActionResult<IEnumerable<CustomerDto>> GetAll([FromQuery] string? search = null)
        {
            using var session = new DbSession(_factory);
            var ctx = new SparePartsDataContext(session);
            var all = ctx.GetAllCustomers();
            if (!string.IsNullOrWhiteSpace(search))
            {
                all = all.Where(c => c.Name.Contains(search, StringComparison.OrdinalIgnoreCase));
            }

            return Ok(all.Select(c => new CustomerDto
            {
                Id = c.Id,
                Name = c.Name,
                Phone = c.Phone,
                Email = c.Email,
                Address = c.Address,
                TaxNumber = c.TaxNumber,
                OpeningBalance = c.OpeningBalance
            }));
        }

        [HttpPost]
        public ActionResult<int> Create([FromBody] CreateCustomerRequest req)
        {
            using var session = new DbSession(_factory);
            var ctx = new SparePartsDataContext(session);
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
            var id = ctx.InsertCustomer(customer);
            session.Commit();
            return Ok(id);
        }

        [HttpPut("{id:int}")]
        public IActionResult Update(int id, [FromBody] CreateCustomerRequest req)
        {
            using var session = new DbSession(_factory);
            var updated = session.Connection.Execute(
                @"UPDATE Customers
                  SET Name = @Name, Phone = @Phone, Email = @Email, Address = @Address,
                      TaxNumber = @TaxNumber, OpeningBalance = @OpeningBalance,
                      ModifiedAt = @Now, ModifiedByUserId = @UserId
                  WHERE Id = @Id",
                new
                {
                    Id = id,
                    req.Name,
                    req.Phone,
                    req.Email,
                    req.Address,
                    req.TaxNumber,
                    req.OpeningBalance,
                    Now = DateTime.UtcNow,
                    UserId = GetUserId()
                },
                session.Transaction);
            if (updated == 0) return NotFound();
            session.Commit();
            return NoContent();
        }

        [HttpDelete("{id:int}")]
        public IActionResult Delete(int id)
        {
            using var session = new DbSession(_factory);
            var deleted = session.Connection.Execute(
                "DELETE FROM Customers WHERE Id = @Id",
                new { Id = id },
                session.Transaction);
            if (deleted == 0) return NotFound();
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
