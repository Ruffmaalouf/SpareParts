using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpareParts.Domain.Inventory;
using SpareParts.Domain.MasterData;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Api.Controllers
{
    [ApiController]
    [Route("api/brands")]
    [Authorize]
    public class BrandsController : ControllerBase
    {
        private readonly ISqlConnectionFactory _factory;
        public BrandsController(ISqlConnectionFactory factory) => _factory = factory;

        [HttpGet]
        public ActionResult<IEnumerable<BrandDto>> GetAll([FromQuery] int? page = null, [FromQuery] int? pageSize = null)
        {

            using var session = new DbSession(_factory);
            var brandsRepository = new BrandsRepository(session);
            var projected = brandsRepository.GetAll().Select(b => new BrandDto
            {
                Id = b.Id,
                Name = b.Name,
                IsActive = b.IsActive
            });

            if (!page.HasValue && !pageSize.HasValue)
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
        public ActionResult<int> Create([FromBody] CreateBrandRequest req)
        {
            using var session = new DbSession(_factory);
            var brandsRepository = new BrandsRepository(session);
            var brand = new Domain.MasterData.Brand
            {
                Name = req.Name,
                IsActive = req.IsActive,
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = GetUserId()
            };
            var id = brandsRepository.Insert(brand);
            session.Commit();
            return Ok(id);
        }

        [HttpPut("{id:int}")]
        public IActionResult Update(int id, [FromBody] CreateBrandRequest req)
        {
            using var session = new DbSession(_factory);
            var brandsRepository = new BrandsRepository(session);
            if (!brandsRepository.Update(id, req.Name, req.IsActive, GetUserId())) return NotFound();
            session.Commit();
            return NoContent();
        }

        [HttpDelete("{id:int}")]
        public IActionResult Delete(int id)
        {
            using var session = new DbSession(_factory);
            var brandsRepository = new BrandsRepository(session);
            if (!brandsRepository.Delete(id)) return NotFound();
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
