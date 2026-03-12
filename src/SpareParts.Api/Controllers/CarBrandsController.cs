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
        public bool   HasLogo     { get; set; }   // true if LogoData != NULL
        // ImageUrl built by controller: /api/carbrands/{id}/logo
    }

    public class CarModelDto
    {
        public int     Id          { get; set; }
        public int     CarBrandId  { get; set; }
        public string  Name        { get; set; } = string.Empty;
        public string? Year        { get; set; }
        public string? EngineType  { get; set; }
        public decimal BasePrice   { get; set; }
        public bool    IsActive    { get; set; }
        public bool    HasImage    { get; set; }
    }

    public class UploadLogoRequest
    {
        public IFormFile Image { get; set; } = null!;
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

        // ── GET /api/carbrands  ───────────────────────────────────────────────
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CarBrandDto>>> GetAll()
        {
            await using var conn = GetConn();
            var rows = await conn.QueryAsync(
                @"SELECT Id, Name, Country, RegionGroup, IsActive, SortOrder,
                         CASE WHEN LogoData IS NOT NULL THEN 1 ELSE 0 END AS HasLogo
                  FROM   CarBrands
                  WHERE  IsActive = 1
                  ORDER  BY SortOrder, Name");

            var result = rows.Select(r => new CarBrandDto
            {
                Id          = (int)r.Id,
                Name        = (string)r.Name,
                Country     = (string)r.Country,
                RegionGroup = (string)r.RegionGroup,
                IsActive    = (bool)r.IsActive,
                SortOrder   = (int)r.SortOrder,
                HasLogo     = (int)r.HasLogo == 1
            });
            return Ok(result);
        }

        // ── GET /api/carbrands/{id}/logo  — returns raw image bytes ──────────
        [HttpGet("{id:int}/logo")]
        [AllowAnonymous]   // let the WPF client fetch images without re-auth
        public async Task<IActionResult> GetLogo(int id)
        {
            await using var conn = GetConn();
            var row = await conn.QueryFirstOrDefaultAsync(
                "SELECT LogoData, LogoMimeType FROM CarBrands WHERE Id = @Id",
                new { Id = id });

            if (row == null || row.LogoData == null)
                return NotFound();

            return File((byte[])row.LogoData, (string)row.LogoMimeType);
        }

        // ── POST /api/carbrands/{id}/logo  — upload image (multipart) ────────
        [HttpPost("{id:int}/logo")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> UploadLogo(int id, IFormFile image)
        {
            if (image == null || image.Length == 0)
                return BadRequest("No image provided.");
            if (image.Length > 2 * 1024 * 1024)
                return BadRequest("Image must be ≤ 2 MB.");

            await using var ms = new MemoryStream();
            await image.CopyToAsync(ms);
            var bytes    = ms.ToArray();
            var mimeType = image.ContentType;

            await using var conn = GetConn();
            int updated = await conn.ExecuteAsync(
                @"UPDATE CarBrands
                  SET    LogoData = @Data, LogoMimeType = @Mime, ModifiedAt = @Now
                  WHERE  Id = @Id",
                new { Data = bytes, Mime = mimeType, Now = DateTime.UtcNow, Id = id });

            if (updated == 0) return NotFound();
            return NoContent();
        }

        // ── POST /api/carbrands  — create new brand ───────────────────────────
        [HttpPost]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<ActionResult<int>> Create([FromBody] CreateCarBrandRequest req)
        {
            await using var conn = GetConn();
            var id = await conn.ExecuteScalarAsync<int>(
                @"INSERT INTO CarBrands (Name,Country,RegionGroup,SortOrder,CreatedByUserId)
                  VALUES (@Name,@Country,@RegionGroup,@SortOrder,@UserId);
                  SELECT CAST(SCOPE_IDENTITY() AS INT);",
                new
                {
                    req.Name, req.Country, req.RegionGroup,
                    req.SortOrder,
                    UserId = GetUserId()
                });
            return Ok(id);
        }

        private System.Data.SqlClient.SqlConnection GetConn()
            => (System.Data.SqlClient.SqlConnection)_factory.CreateConnection();

        private int GetUserId()
        {
            var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            return claim != null ? int.Parse(claim.Value) : 1;
        }
    }

    public class CreateCarBrandRequest
    {
        public string Name        { get; set; } = string.Empty;
        public string Country     { get; set; } = string.Empty;
        public string RegionGroup { get; set; } = string.Empty;
        public int    SortOrder   { get; set; }
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  CAR MODELS CONTROLLER
    // ══════════════════════════════════════════════════════════════════════════

    //[ApiController]
    //[Route("api/carmodels")]
    //[Authorize]
    //public class CarModelsController : ControllerBase
    //{
    //    private readonly ISqlConnectionFactory _factory;
    //    public CarModelsController(ISqlConnectionFactory factory) => _factory = factory;

    //    // ── GET /api/carmodels?brandId=1  ─────────────────────────────────────
    //    [HttpGet]
    //    public async Task<ActionResult<IEnumerable<CarModelDto>>> GetAll([FromQuery] int? brandId)
    //    {
    //        await using var conn = GetConn();
    //        var sql = brandId.HasValue
    //            ? @"SELECT Id, CarBrandId, Name, Year, EngineType, BasePrice, IsActive,
    //                       CASE WHEN ImageData IS NOT NULL THEN 1 ELSE 0 END AS HasImage
    //                FROM   CarModels
    //                WHERE  CarBrandId = @BrandId AND IsActive = 1
    //                ORDER  BY Name"
    //            : @"SELECT Id, CarBrandId, Name, Year, EngineType, BasePrice, IsActive,
    //                       CASE WHEN ImageData IS NOT NULL THEN 1 ELSE 0 END AS HasImage
    //                FROM   CarModels WHERE IsActive = 1 ORDER BY Name";

    //        var rows = await conn.QueryAsync(sql, new { BrandId = brandId });

    //        var result = rows.Select(r => new CarModelDto
    //        {
    //            Id         = (int)r.Id,
    //            CarBrandId = (int)r.CarBrandId,
    //            Name       = (string)r.Name,
    //            Year       = (string?)r.Year,
    //            EngineType = (string?)r.EngineType,
    //            BasePrice  = (decimal)r.BasePrice,
    //            IsActive   = (bool)r.IsActive,
    //            HasImage   = (int)r.HasImage == 1
    //        });
    //        return Ok(result);
    //    }

    //    // ── GET /api/carmodels/{id}/image  ────────────────────────────────────
    //    [HttpGet("{id:int}/image")]
    //    [AllowAnonymous]
    //    public async Task<IActionResult> GetImage(int id)
    //    {
    //        await using var conn = GetConn();
    //        var row = await conn.QueryFirstOrDefaultAsync(
    //            "SELECT ImageData, ImageMimeType FROM CarModels WHERE Id = @Id",
    //            new { Id = id });

    //        if (row == null || row.ImageData == null)
    //            return NotFound();

    //        return File((byte[])row.ImageData, (string)row.ImageMimeType);
    //    }

    //    // ── POST /api/carmodels/{id}/image  ───────────────────────────────────
    //    [HttpPost("{id:int}/image")]
    //    [Authorize(Roles = "Admin,Manager")]
    //    public async Task<IActionResult> UploadImage(int id, IFormFile image)
    //    {
    //        if (image == null || image.Length == 0) return BadRequest("No image.");
    //        if (image.Length > 4 * 1024 * 1024)    return BadRequest("Image ≤ 4 MB.");

    //        await using var ms = new MemoryStream();
    //        await image.CopyToAsync(ms);

    //        await using var conn = GetConn();
    //        int updated = await conn.ExecuteAsync(
    //            @"UPDATE CarModels
    //              SET    ImageData = @Data, ImageMimeType = @Mime, ModifiedAt = @Now
    //              WHERE  Id = @Id",
    //            new { Data = ms.ToArray(), Mime = image.ContentType,
    //                  Now = DateTime.UtcNow, Id = id });

    //        if (updated == 0) return NotFound();
    //        return NoContent();
    //    }

    //    // ── POST /api/carmodels  — create ─────────────────────────────────────
    //    [HttpPost]
    //    [Authorize(Roles = "Admin,Manager")]
    //    public async Task<ActionResult<int>> Create([FromBody] CreateCarModelRequest req)
    //    {
    //        await using var conn = GetConn();
    //        var id = await conn.ExecuteScalarAsync<int>(
    //            @"INSERT INTO CarModels (CarBrandId,Name,Year,EngineType,BasePrice,CreatedByUserId)
    //              VALUES (@CarBrandId,@Name,@Year,@EngineType,@BasePrice,@UserId);
    //              SELECT CAST(SCOPE_IDENTITY() AS INT);",
    //            new { req.CarBrandId, req.Name, req.Year,
    //                  req.EngineType, req.BasePrice, UserId = GetUserId() });
    //        return Ok(id);
    //    }

    //    private System.Data.SqlClient.SqlConnection GetConn()
    //        => (System.Data.SqlClient.SqlConnection)_factory.CreateConnection();

    //    private int GetUserId()
    //    {
    //        var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
    //        return claim != null ? int.Parse(claim.Value) : 1;
    //    }
    //}

    //public class CreateCarModelRequest
    //{
    //    public int     CarBrandId  { get; set; }
    //    public string  Name        { get; set; } = string.Empty;
    //    public string? Year        { get; set; }
    //    public string? EngineType  { get; set; }
    //    public decimal BasePrice   { get; set; }
    //}

}
