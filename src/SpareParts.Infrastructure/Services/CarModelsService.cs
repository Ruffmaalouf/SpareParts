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
            ? @"SELECT Id, CarBrandId, Name, Year, EngineType, BasePrice, IsActive,
                       CAST(CASE WHEN ImageData IS NOT NULL THEN 1 ELSE 0 END AS BIT) AS HasImage
                FROM CarModels
                WHERE CarBrandId = @BrandId AND IsActive = 1
                ORDER BY Name"
            : @"SELECT Id, CarBrandId, Name, Year, EngineType, BasePrice, IsActive,
                       CAST(CASE WHEN ImageData IS NOT NULL THEN 1 ELSE 0 END AS BIT) AS HasImage
                FROM CarModels
                WHERE IsActive = 1
                ORDER BY Name";

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
        using var conn = _factory.CreateConnection();
        return conn.ExecuteScalar<int>(
            @"INSERT INTO CarModels (CarBrandId, Name, Year, EngineType, BasePrice, CreatedByUserId)
              VALUES (@CarBrandId, @Name, @Year, @EngineType, @BasePrice, @UserId);
              SELECT CAST(SCOPE_IDENTITY() AS INT);",
            new
            {
                request.CarBrandId,
                request.Name,
                request.Year,
                request.EngineType,
                request.BasePrice,
                UserId = userId
            });
    }

    public void Update(int id, CreateCarModelRequest request)
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
                request.CarBrandId,
                request.Name,
                request.Year,
                request.EngineType,
                request.BasePrice,
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
        var deleted = conn.Execute("DELETE FROM CarModels WHERE Id = @Id", new { Id = id });
        if (deleted == 0)
        {
            throw new NotFoundException("Car model not found.");
        }
    }
}
