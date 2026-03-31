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
        public ActionResult<IEnumerable<SupplierDto>> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 100)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 500);

            using var session = new DbSession(_factory);
            var suppliersRepository = new SuppliersRepository(session);
            var projected = suppliersRepository.GetAll().Select(s => new SupplierDto
            {
                Id = s.Id,
                Name = s.Name,
                Phone = s.Phone,
                Email = s.Email,
                Address = s.Address,
                TaxNumber = s.TaxNumber,
                OpeningBalance = s.OpeningBalance
            });

            Response.Headers["X-Page"] = page.ToString();
            Response.Headers["X-Page-Size"] = pageSize.ToString();
            Response.Headers["X-Total-Count"] = projected.Count().ToString();
            return Ok(projected.Skip((page - 1) * pageSize).Take(pageSize));
        }

        [HttpPost]
        public ActionResult<int> Create([FromBody] CreateSupplierRequest req)
        {
            using var session = new DbSession(_factory);
            var suppliersRepository = new SuppliersRepository(session);
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
            var id = suppliersRepository.Insert(supplier);
            session.Commit();
            return Ok(id);
        }

        [HttpPut("{id:int}")]
        public IActionResult Update(int id, [FromBody] CreateSupplierRequest req)
        {
            using var session = new DbSession(_factory);
            var suppliersRepository = new SuppliersRepository(session);
            if (!suppliersRepository.Update(id, req, GetUserId())) return NotFound();
            session.Commit();
            return NoContent();
        }

        [HttpDelete("{id:int}")]
        public IActionResult Delete(int id)
        {
            using var session = new DbSession(_factory);
            var suppliersRepository = new SuppliersRepository(session);
            if (!suppliersRepository.Delete(id)) return NotFound();
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
