using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Api.Controllers
{
    // ══════════════════════════════════════════════════════════════════════════
    //  DTOs
    // ══════════════════════════════════════════════════════════════════════════

    public class CarBrandDto
    {
        public int    Id          { get; set; }
        public string Name        { get; set; } = string.Empty;
        public string Country     { get; set; } = string.Empty;
        public string RegionGroup { get; set; } = string.Empty;
        public bool   IsActive    { get; set; }
        public int    SortOrder   { get; set; }
        public bool   HasLogo     { get; set; }
    }

    public class CarModelDto
    {
        public int     Id         { get; set; }
        public int     CarBrandId { get; set; }
        public string  Name       { get; set; } = string.Empty;
        public string? Year       { get; set; }
        public string? EngineType { get; set; }
        public decimal BasePrice  { get; set; }
        public bool    IsActive   { get; set; }
        public bool    HasImage   { get; set; }
    }

    public class CreateCarBrandRequest
    {
        public string Name        { get; set; } = string.Empty;
        public string Country     { get; set; } = string.Empty;
        public string RegionGroup { get; set; } = string.Empty;
        public int    SortOrder   { get; set; }
    }

    public class CreateCarModelRequest
    {
        public int     CarBrandId  { get; set; }
        public string  Name        { get; set; } = string.Empty;
        public string? Year        { get; set; }
        public string? EngineType  { get; set; }
        public decimal BasePrice   { get; set; }
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  CAR BRANDS CONTROLLER
    // ══════════════════════════════════════════════════════════════════════════

    [ApiController]
    [Route("api/carbrands")]
    [Authorize]
    public class CarBrandsController : ControllerBase
    {
        private readonly ISqlConnectionFactory _factory;
        public CarBrandsController(ISqlConnectionFactory factory) => _factory = factory;

        /// <summary>GET /api/carbrands — all active brands with group info</summary>
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

        /// <summary>GET /api/carbrands/{id}/logo — raw logo bytes (AllowAnonymous for WPF img cache)</summary>
        [HttpGet("{id:int}/logo")]
        [AllowAnonymous]
        public ActionResult GetLogo(int id)
        {
            using var conn = _factory.CreateConnection();
            var row = conn.QueryFirstOrDefault(
                "SELECT LogoData, LogoMimeType FROM CarBrands WHERE Id = @Id",
                new { Id = id });

            if (row == null || row.LogoData == null)
                return NotFound();

            return File((byte[])row.LogoData, (string)row.LogoMimeType);
        }

        /// <summary>POST /api/carbrands/{id}/logo — upload logo (multipart)</summary>
        [HttpPost("{id:int}/logo")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> UploadLogo(int id, IFormFile image)
        {
            if (image == null || image.Length == 0)
                return BadRequest("No image provided.");
            if (image.Length > 2 * 1024 * 1024)
                return BadRequest("Image must be ≤ 2 MB.");

            using var ms = new MemoryStream();
            await image.CopyToAsync(ms);

            using var conn = _factory.CreateConnection();
            int updated = conn.Execute(
                @"UPDATE CarBrands
                  SET    LogoData = @Data, LogoMimeType = @Mime, ModifiedAt = @Now
                  WHERE  Id = @Id",
                new { Data = ms.ToArray(), Mime = image.ContentType,
                      Now = DateTime.UtcNow, Id = id });

            if (updated == 0) return NotFound();
            return NoContent();
        }

        /// <summary>POST /api/carbrands — create new brand</summary>
        [HttpPost]
        [Authorize(Roles = "Admin,Manager")]
        public ActionResult<int> Create([FromBody] CreateCarBrandRequest req)
        {
            using var conn = _factory.CreateConnection();
            var id = conn.ExecuteScalar<int>(
                @"INSERT INTO CarBrands (Name, Country, RegionGroup, SortOrder, CreatedByUserId)
                  VALUES (@Name, @Country, @RegionGroup, @SortOrder, @UserId);
                  SELECT CAST(SCOPE_IDENTITY() AS INT);",
                new { req.Name, req.Country, req.RegionGroup, req.SortOrder,
                      UserId = GetUserId() });
            return Ok(id);
        }

        private int GetUserId()
        {
            var c = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            return c != null ? int.Parse(c.Value) : 1;
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  CAR MODELS CONTROLLER
    // ══════════════════════════════════════════════════════════════════════════

    [ApiController]
    [Route("api/carmodels")]
    [Authorize]
    public class CarModelsController : ControllerBase
    {
        private readonly ISqlConnectionFactory _factory;
        public CarModelsController(ISqlConnectionFactory factory) => _factory = factory;

        /// <summary>
        /// GET /api/carmodels?brandId=1
        /// When brandId is supplied, ONLY that brand's cars are returned.
        /// This is the fix for "all cars showing when clicking Mercedes".
        /// </summary>
        [HttpGet]
        public ActionResult<IEnumerable<CarModelDto>> GetAll([FromQuery] int? brandId)
        {
            using var conn = _factory.CreateConnection();

            // Always filter by brandId when provided — this fixes the Mercedes bug
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

        /// <summary>GET /api/carmodels/{id}/image — raw image bytes</summary>
        [HttpGet("{id:int}/image")]
        [AllowAnonymous]
        public ActionResult GetImage(int id)
        {
            using var conn = _factory.CreateConnection();
            var row = conn.QueryFirstOrDefault(
                "SELECT ImageData, ImageMimeType FROM CarModels WHERE Id = @Id",
                new { Id = id });

            if (row == null || row.ImageData == null)
                return NotFound();

            return File((byte[])row.ImageData, (string)row.ImageMimeType);
        }

        /// <summary>POST /api/carmodels/{id}/image — upload car image</summary>
        [HttpPost("{id:int}/image")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> UploadImage(int id, IFormFile image)
        {
            if (image == null || image.Length == 0) return BadRequest("No image.");
            if (image.Length > 4 * 1024 * 1024)    return BadRequest("Image must be ≤ 4 MB.");

            using var ms = new MemoryStream();
            await image.CopyToAsync(ms);

            using var conn = _factory.CreateConnection();
            int updated = conn.Execute(
                @"UPDATE CarModels
                  SET    ImageData = @Data, ImageMimeType = @Mime, ModifiedAt = @Now
                  WHERE  Id = @Id",
                new { Data = ms.ToArray(), Mime = image.ContentType,
                      Now = DateTime.UtcNow, Id = id });

            if (updated == 0) return NotFound();
            return NoContent();
        }

        /// <summary>POST /api/carmodels — create new model</summary>
        [HttpPost]
        [Authorize(Roles = "Admin,Manager")]
        public ActionResult<int> Create([FromBody] CreateCarModelRequest req)
        {
            using var conn = _factory.CreateConnection();
            var id = conn.ExecuteScalar<int>(
                @"INSERT INTO CarModels (CarBrandId, Name, Year, EngineType, BasePrice, CreatedByUserId)
                  VALUES (@CarBrandId, @Name, @Year, @EngineType, @BasePrice, @UserId);
                  SELECT CAST(SCOPE_IDENTITY() AS INT);",
                new { req.CarBrandId, req.Name, req.Year, req.EngineType,
                      req.BasePrice, UserId = GetUserId() });
            return Ok(id);
        }

        private int GetUserId()
        {
            var c = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            return c != null ? int.Parse(c.Value) : 1;
        }
    }
}
