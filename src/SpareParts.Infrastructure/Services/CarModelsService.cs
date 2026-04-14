using Dapper;
using SpareParts.Domain.Cars;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Infrastructure.Services;

public sealed class CarModelsService
{
    private readonly ISqlConnectionFactory _factory;

    public CarModelsService(ISqlConnectionFactory factory)
    {
        _factory = factory;
    }

    public IEnumerable<CarModelDto> GetAll(int? brandId)
    {
        using var conn = _factory.CreateConnection();

        var sql = brandId.HasValue
            ? @"SELECT cm.Id,
                       cm.CarBrandId,
                       cb.Name AS CarBrandName,
                       cm.Name,
                       COALESCE(NULLIF(LTRIM(RTRIM(cm.BodyType)), N''), N'') AS BodyType,
                       cm.IsActive,
                       CAST(CASE WHEN cm.ImageData IS NOT NULL THEN 1 ELSE 0 END AS BIT) AS HasImage
                FROM CarModels cm
                INNER JOIN CarBrands cb ON cb.Id = cm.CarBrandId
                WHERE cm.CarBrandId = @BrandId AND cm.IsActive = 1
                ORDER BY cb.Name, cm.Name"
            : @"SELECT cm.Id,
                       cm.CarBrandId,
                       cb.Name AS CarBrandName,
                       cm.Name,
                       COALESCE(NULLIF(LTRIM(RTRIM(cm.BodyType)), N''), N'') AS BodyType,
                       cm.IsActive,
                       CAST(CASE WHEN cm.ImageData IS NOT NULL THEN 1 ELSE 0 END AS BIT) AS HasImage
                FROM CarModels cm
                INNER JOIN CarBrands cb ON cb.Id = cm.CarBrandId
                WHERE cm.IsActive = 1
                ORDER BY cb.Name, cm.Name";

        return conn.Query<CarModelDto>(sql, new { BrandId = brandId });
    }

    public (byte[] Data, string MimeType) GetImage(int id)
    {
        using var conn = _factory.CreateConnection();
        var row = conn.QueryFirstOrDefault<(byte[]? ImageData, string? ImageMimeType)>(
            "SELECT ImageData, ImageMimeType FROM CarModels WHERE Id = @Id",
            new { Id = id });

        if (row.ImageData == null || string.IsNullOrWhiteSpace(row.ImageMimeType))
        {
            throw new NotFoundException("Image not found.");
        }

        return (row.ImageData, row.ImageMimeType);
    }

    public void UploadImage(int id, byte[] data, string mimeType)
    {
        using var conn = _factory.CreateConnection();
        var updated = conn.Execute(
            @"UPDATE CarModels
              SET ImageData = @Data, ImageMimeType = @Mime, ModifiedAt = @Now
              WHERE Id = @Id",
            new { Data = data, Mime = mimeType, Now = DateTime.UtcNow, Id = id });

        if (updated == 0)
        {
            throw new NotFoundException("Car model not found.");
        }
    }

    public int Create(CreateCarModelRequest request, int userId)
    {
        var normalizedName = request.Name.Trim();
        var normalizedBodyType = string.IsNullOrWhiteSpace(request.BodyType) ? null : request.BodyType.Trim();

        using var conn = _factory.CreateConnection();
        return conn.ExecuteScalar<int>(
            @"INSERT INTO CarModels (CarBrandId, Name, BodyType, CreatedByUserId)
              VALUES (@CarBrandId, @Name, @BodyType, @UserId);
              SELECT CAST(SCOPE_IDENTITY() AS INT);",
            new
            {
                request.CarBrandId,
                Name = normalizedName,
                BodyType = normalizedBodyType,
                UserId = userId
            });
    }

    public void Update(int id, CreateCarModelRequest request)
    {
        var normalizedName = request.Name.Trim();
        var normalizedBodyType = string.IsNullOrWhiteSpace(request.BodyType) ? null : request.BodyType.Trim();

        using var conn = _factory.CreateConnection();
        var updated = conn.Execute(
            @"UPDATE CarModels
              SET CarBrandId = @CarBrandId,
                  Name = @Name,
                  BodyType = @BodyType,
                  ModifiedAt = @Now
              WHERE Id = @Id",
            new
            {
                Id = id,
                request.CarBrandId,
                Name = normalizedName,
                BodyType = normalizedBodyType,
                Now = DateTime.UtcNow
            });

        if (updated == 0)
        {
            throw new NotFoundException("Car model not found.");
        }
    }

    public void Delete(int id)
    {
        using var conn = _factory.CreateConnection();
        var linkedUsedCars = conn.ExecuteScalar<int>(
            "SELECT COUNT(1) FROM dbo.UsedCars WHERE CarModelId = @Id",
            new { Id = id });
        if (linkedUsedCars > 0)
        {
            throw new ValidationException("Car model cannot be deleted while used car rows still reference it.");
        }

        var deleted = conn.Execute("DELETE FROM CarModels WHERE Id = @Id", new { Id = id });
        if (deleted == 0)
        {
            throw new NotFoundException("Car model not found.");
        }
    }
}
