using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpareParts.Domain.Cars;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Api.Controllers
{
    [ApiController]
    [Route("api/carbrands")]
    [Authorize]
    public class CarBrandsController : ControllerBase
    {
        private readonly ISqlConnectionFactory _factory;
        public CarBrandsController(ISqlConnectionFactory factory) => _factory = factory;

        [HttpGet]
        public ActionResult<IEnumerable<CarBrandDto>> GetAll()
        {
            using var conn = _factory.CreateConnection();
            var rows = conn.Query<CarBrandDto>(
                @"SELECT Id, Name, Country, RegionGroup, IsActive, SortOrder,
                         CAST(CASE WHEN LogoData IS NOT NULL THEN 1 ELSE 0 END AS BIT) AS HasLogo
                  FROM   CarBrands
                  WHERE  IsActive = 1
                  ORDER  BY SortOrder, Name");
            return Ok(rows);
        }

        [HttpGet("{id:int}/logo")]
        [AllowAnonymous]
        public ActionResult GetLogo(int id)
        {
            using var conn = _factory.CreateConnection();
            var row = conn.QueryFirstOrDefault(
                "SELECT LogoData, LogoMimeType FROM CarBrands WHERE Id = @Id",
                new { Id = id });

            if (row == null || row.LogoData == null)
            {
                return NotFound();
            }

            return File((byte[])row.LogoData, (string)row.LogoMimeType);
        }

        [HttpPost("{id:int}/logo")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> UploadLogo(int id, IFormFile image)
        {
            if (image == null || image.Length == 0)
            {
                return BadRequest("No image provided.");
            }

            if (image.Length > 2 * 1024 * 1024)
            {
                return BadRequest("Image must be ≤ 2 MB.");
            }

            using var ms = new MemoryStream();
            await image.CopyToAsync(ms);

            using var conn = _factory.CreateConnection();
            int updated = conn.Execute(
                @"UPDATE CarBrands
                  SET    LogoData = @Data, LogoMimeType = @Mime, ModifiedAt = @Now
                  WHERE  Id = @Id",
                new { Data = ms.ToArray(), Mime = image.ContentType, Now = DateTime.UtcNow, Id = id });

            if (updated == 0) return NotFound();
            return NoContent();
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Manager")]
        public ActionResult<int> Create([FromBody] CreateCarBrandRequest req)
        {
            using var conn = _factory.CreateConnection();
            var id = conn.ExecuteScalar<int>(
                @"INSERT INTO CarBrands (Name, Country, RegionGroup, SortOrder, CreatedByUserId)
                  VALUES (@Name, @Country, @RegionGroup, @SortOrder, @UserId);
                  SELECT CAST(SCOPE_IDENTITY() AS INT);",
                new
                {
                    req.Name,
                    req.Country,
                    req.RegionGroup,
                    req.SortOrder,
                    UserId = GetUserId()
                });
            return Ok(id);
        }

        private int GetUserId()
        {
            var c = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            return c != null ? int.Parse(c.Value) : 1;
        }
    }
}
