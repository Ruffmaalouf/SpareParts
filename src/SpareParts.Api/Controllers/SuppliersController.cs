using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpareParts.Domain.BusinessPartners;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Api.Controllers
{
    [ApiController]
    [Route("api/suppliers")]
    [Authorize]
    public class SuppliersController : ControllerBase
    {
        private readonly ISqlConnectionFactory _factory;
        public SuppliersController(ISqlConnectionFactory factory) => _factory = factory;

        [HttpGet]
        public ActionResult<IEnumerable<SupplierDto>> GetAll()
        {
            using var session = new DbSession(_factory);
            var ctx = new SparePartsDataContext(session);
            return Ok(ctx.GetAllSuppliers().Select(s => new SupplierDto
            {
                Id = s.Id,
                Name = s.Name,
                Phone = s.Phone,
                Email = s.Email,
                Address = s.Address,
                TaxNumber = s.TaxNumber,
                OpeningBalance = s.OpeningBalance
            }));
        }

        [HttpPost]
        public ActionResult<int> Create([FromBody] CreateSupplierRequest req)
        {
            using var session = new DbSession(_factory);
            var ctx = new SparePartsDataContext(session);
            var supplier = new Supplier
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
            var id = ctx.InsertSupplier(supplier);
            session.Commit();
            return Ok(id);
        }

        [HttpPut("{id:int}")]
        public IActionResult Update(int id, [FromBody] CreateSupplierRequest req)
        {
            using var session = new DbSession(_factory);
            var updated = session.Connection.Execute(
                @"UPDATE Suppliers
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
                "DELETE FROM Suppliers WHERE Id = @Id",
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
