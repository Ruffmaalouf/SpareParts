using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpareParts.Domain.Cars;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Api.Controllers
{
    [ApiController]
    [Route("api/carmodels")]
    [Authorize]
    public class CarModelsController : ControllerBase
    {
        private readonly ISqlConnectionFactory _factory;
        public CarModelsController(ISqlConnectionFactory factory) => _factory = factory;

        [HttpGet]
        public ActionResult<IEnumerable<CarModelDto>> GetAll([FromQuery] int? brandId)
        {
            using var conn = _factory.CreateConnection();

            string sql;
            object param;

            if (brandId.HasValue)
            {
                sql = @"SELECT Id, CarBrandId, Name, Year, EngineType, BasePrice, IsActive,
                               CAST(CASE WHEN ImageData IS NOT NULL THEN 1 ELSE 0 END AS BIT) AS HasImage
                        FROM   CarModels
                        WHERE  CarBrandId = @BrandId AND IsActive = 1
                        ORDER  BY Name";
                param = new { BrandId = brandId.Value };
            }
            else
            {
                sql = @"SELECT Id, CarBrandId, Name, Year, EngineType, BasePrice, IsActive,
                               CAST(CASE WHEN ImageData IS NOT NULL THEN 1 ELSE 0 END AS BIT) AS HasImage
                        FROM   CarModels
                        WHERE  IsActive = 1
                        ORDER  BY Name";
                param = new { };
            }

            var rows = conn.Query<CarModelDto>(sql, param);
            return Ok(rows);
        }

        [HttpGet("{id:int}/image")]
        [AllowAnonymous]
        public ActionResult GetImage(int id)
        {
            using var conn = _factory.CreateConnection();
            var row = conn.QueryFirstOrDefault(
                "SELECT ImageData, ImageMimeType FROM CarModels WHERE Id = @Id",
                new { Id = id });

            if (row == null || row.ImageData == null)
            {
                return NotFound();
            }

            return File((byte[])row.ImageData, (string)row.ImageMimeType);
        }

        [HttpPost("{id:int}/image")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> UploadImage(int id, IFormFile image)
        {
            if (image == null || image.Length == 0) return BadRequest("No image.");
            if (image.Length > 4 * 1024 * 1024) return BadRequest("Image must be ≤ 4 MB.");

            using var ms = new MemoryStream();
            await image.CopyToAsync(ms);

            using var conn = _factory.CreateConnection();
            int updated = conn.Execute(
                @"UPDATE CarModels
                  SET    ImageData = @Data, ImageMimeType = @Mime, ModifiedAt = @Now
                  WHERE  Id = @Id",
                new { Data = ms.ToArray(), Mime = image.ContentType, Now = DateTime.UtcNow, Id = id });

            if (updated == 0) return NotFound();
            return NoContent();
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Manager")]
        public ActionResult<int> Create([FromBody] CreateCarModelRequest req)
        {
            using var conn = _factory.CreateConnection();
            var id = conn.ExecuteScalar<int>(
                @"INSERT INTO CarModels (CarBrandId, Name, Year, EngineType, BasePrice, CreatedByUserId)
                  VALUES (@CarBrandId, @Name, @Year, @EngineType, @BasePrice, @UserId);
                  SELECT CAST(SCOPE_IDENTITY() AS INT);",
                new
                {
                    req.CarBrandId,
                    req.Name,
                    req.Year,
                    req.EngineType,
                    req.BasePrice,
                    UserId = GetUserId()
                });
            return Ok(id);
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin,Manager")]
        public IActionResult Update(int id, [FromBody] CreateCarModelRequest req)
        {
            using var conn = _factory.CreateConnection();
            var updated = conn.Execute(
                @"UPDATE CarModels
                  SET CarBrandId = @CarBrandId, Name = @Name, Year = @Year,
                      EngineType = @EngineType, BasePrice = @BasePrice, ModifiedAt = @Now
                  WHERE Id = @Id",
                new
                {
                    Id = id,
                    req.CarBrandId,
                    req.Name,
                    req.Year,
                    req.EngineType,
                    req.BasePrice,
                    Now = DateTime.UtcNow
                });
            if (updated == 0) return NotFound();
            return NoContent();
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin,Manager")]
        public IActionResult Delete(int id)
        {
            using var conn = _factory.CreateConnection();
            var deleted = conn.Execute("DELETE FROM CarModels WHERE Id = @Id", new { Id = id });
            if (deleted == 0) return NotFound();
            return NoContent();
        }

        private int GetUserId()
        {
            var c = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            return c != null ? int.Parse(c.Value) : 1;
        }
    }
}
