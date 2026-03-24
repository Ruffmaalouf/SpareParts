using Dapper;
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
        public ActionResult<IEnumerable<BrandDto>> GetAll()
        {
            using var session = new DbSession(_factory);
            var ctx = new SparePartsDataContext(session);
            return Ok(ctx.GetAllBrands().Select(b => new BrandDto
            {
                Id = b.Id,
                Name = b.Name,
                IsActive = b.IsActive
            }));
        }

        [HttpPost]
        public ActionResult<int> Create([FromBody] CreateBrandRequest req)
        {
            using var session = new DbSession(_factory);
            var ctx = new SparePartsDataContext(session);
            var brand = new Domain.MasterData.Brand
            {
                Name = req.Name,
                IsActive = req.IsActive,
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = GetUserId()
            };
            var id = ctx.InsertBrand(brand);
            session.Commit();
            return Ok(id);
        }

        [HttpPut("{id:int}")]
        public IActionResult Update(int id, [FromBody] CreateBrandRequest req)
        {
            using var session = new DbSession(_factory);
            var updated = session.Connection.Execute(
                @"UPDATE Brands
                  SET Name = @Name, IsActive = @IsActive,
                      ModifiedAt = @Now, ModifiedByUserId = @UserId
                  WHERE Id = @Id",
                new
                {
                    Id = id,
                    req.Name,
                    req.IsActive,
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
                "DELETE FROM Brands WHERE Id = @Id",
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
