using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpareParts.Domain.Inventory;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Api.Controllers
{
    [ApiController]
    [Route("api/parts")]
    [Authorize]
    public class PartsController : ControllerBase
    {
        private readonly ISqlConnectionFactory _factory;
        public PartsController(ISqlConnectionFactory factory) => _factory = factory;

        [HttpGet]
        public ActionResult<IEnumerable<PartDto>> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 100)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 500);

            using var session = new DbSession(_factory);
            var partsRepository = new PartsRepository(session);
            var projected = partsRepository.GetAllActive().Select(p => new PartDto
            {
                Id = p.Id,
                InternalCode = p.InternalCode,
                Barcode = p.Barcode,
                Name = p.Name,
                OEMNumber = p.OEMNumber,
                Condition = p.Condition,
                CategoryId = p.CategoryId,
                BrandId = p.BrandId,
                CostPrice = p.CostPrice,
                SalePrice = p.SalePrice,
                Currency = p.Currency,
                MinStock = p.MinStock,
                Notes = p.Notes,
                IsActive = p.IsActive
            });

            Response.Headers["X-Page"] = page.ToString();
            Response.Headers["X-Page-Size"] = pageSize.ToString();
            Response.Headers["X-Total-Count"] = projected.Count().ToString();
            return Ok(projected.Skip((page - 1) * pageSize).Take(pageSize));
        }

        [HttpPost]
        public ActionResult<int> Create([FromBody] CreatePartRequest req)
        {
            using var session = new DbSession(_factory);
            var partsRepository = new PartsRepository(session);
            var part = new Domain.Inventory.Part
            {
                InternalCode = req.InternalCode,
                Barcode = req.Barcode,
                Name = req.Name,
                OEMNumber = req.OEMNumber,
                Condition = req.Condition,
                CategoryId = req.CategoryId,
                BrandId = req.BrandId,
                CostPrice = req.CostPrice,
                SalePrice = req.SalePrice,
                Currency = req.Currency,
                MinStock = req.MinStock,
                Notes = req.Notes,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = GetUserId()
            };
            var id = partsRepository.Insert(part);
            session.Commit();
            return Ok(id);
        }

        [HttpPut("{id:int}")]
        public IActionResult Update(int id, [FromBody] CreatePartRequest req)
        {
            using var session = new DbSession(_factory);
            var partsRepository = new PartsRepository(session);
            if (!partsRepository.Update(id, req, GetUserId())) return NotFound();
            session.Commit();
            return NoContent();
        }

        [HttpDelete("{id:int}")]
        public IActionResult Delete(int id)
        {
            using var session = new DbSession(_factory);
            var partsRepository = new PartsRepository(session);
            if (!partsRepository.Delete(id)) return NotFound();
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
