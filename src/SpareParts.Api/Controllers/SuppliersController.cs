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
        public ActionResult<IEnumerable<SupplierDto>> GetAll([FromQuery] int? page = null, [FromQuery] int? pageSize = null)
        {

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

            if (!pageSize.HasValue)
            {
                return Ok(projected);
            }

            var resolvedPage = Math.Max(1, page ?? 1);
            var resolvedPageSize = Math.Clamp(pageSize ?? 100, 1, 500);
            var totalCount = projected.Count();

            Response.Headers["X-Page"] = resolvedPage.ToString();
            Response.Headers["X-Page-Size"] = resolvedPageSize.ToString();
            Response.Headers["X-Total-Count"] = totalCount.ToString();
            return Ok(projected.Skip((resolvedPage - 1) * resolvedPageSize).Take(resolvedPageSize));
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
