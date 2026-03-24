using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpareParts.Domain.Inventory;
using SpareParts.Domain.MasterData;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Api.Controllers
{
    [ApiController]
    [Route("api/categories")]
    [Authorize]
    public class CategoriesController : ControllerBase
    {
        private readonly ISqlConnectionFactory _factory;
        public CategoriesController(ISqlConnectionFactory factory) => _factory = factory;

        [HttpGet]
        public ActionResult<IEnumerable<CategoryDto>> GetAll()
        {
            using var session = new DbSession(_factory);
            var ctx = new SparePartsDataContext(session);
            return Ok(ctx.GetAllCategories().Select(c => new CategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                ParentId = c.ParentId
            }));
        }

        [HttpPost]
        public ActionResult<int> Create([FromBody] CreateCategoryRequest req)
        {
            using var session = new DbSession(_factory);
            var ctx = new SparePartsDataContext(session);
            var cat = new Domain.MasterData.Category
            {
                Name = req.Name,
                ParentId = req.ParentId,
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = GetUserId()
            };
            var id = ctx.InsertCategory(cat);
            session.Commit();
            return Ok(id);
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
