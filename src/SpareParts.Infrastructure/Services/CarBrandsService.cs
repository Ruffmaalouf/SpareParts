using Dapper;
using SpareParts.Domain.Cars;
using SpareParts.Infrastructure.Data;
using System.ComponentModel.DataAnnotations;

namespace SpareParts.Infrastructure.Services;

public sealed class CarBrandsService
{
    private readonly ISqlConnectionFactory _factory;

    public CarBrandsService(ISqlConnectionFactory factory)
    {
        _factory = factory;
    }

    public IEnumerable<CarBrandDto> GetAll()
    {
        using var conn = _factory.CreateConnection();
        return conn.Query<CarBrandDto>(
            @"SELECT Id, Name, Country, RegionGroup, IsActive, SortOrder,
                     CAST(CASE WHEN LogoData IS NOT NULL THEN 1 ELSE 0 END AS BIT) AS HasLogo
              FROM CarBrands
              WHERE IsActive = 1
              ORDER BY SortOrder, Name");
    }

    public (byte[] Data, string MimeType) GetLogo(int id)
    {
        using var conn = _factory.CreateConnection();
        var row = conn.QueryFirstOrDefault<(byte[]? LogoData, string? LogoMimeType)>(
            "SELECT LogoData, LogoMimeType FROM CarBrands WHERE Id = @Id",
            new { Id = id });

        if (row.LogoData == null || string.IsNullOrWhiteSpace(row.LogoMimeType))
        {
            throw new NotFoundException("Logo not found.");
        }

        return (row.LogoData, row.LogoMimeType);
    }

    public void UploadLogo(int id, byte[] data, string mimeType)
    {
        var updated = 0;
        using (var conn = _factory.CreateConnection())
        {
            updated = conn.Execute(
                @"UPDATE CarBrands
                  SET LogoData = @Data, LogoMimeType = @Mime, ModifiedAt = @Now
                  WHERE Id = @Id",
                new { Data = data, Mime = mimeType, Now = DateTime.UtcNow, Id = id });
        }

        if (updated == 0)
        {
            throw new NotFoundException("Car brand not found.");
        }
    }

    public int Create(CreateCarBrandRequest request, int userId)
    {
        using var conn = _factory.CreateConnection();
        return conn.ExecuteScalar<int>(
            @"INSERT INTO CarBrands (Name, Country, RegionGroup, SortOrder, CreatedByUserId)
              VALUES (@Name, @Country, @RegionGroup, @SortOrder, @UserId);
              SELECT CAST(SCOPE_IDENTITY() AS INT);",
            new
            {
                request.Name,
                request.Country,
                request.RegionGroup,
                request.SortOrder,
                UserId = userId
            });
    }

    public void Update(int id, CreateCarBrandRequest request)
    {
        using var conn = _factory.CreateConnection();
        var updated = conn.Execute(
            @"UPDATE CarBrands
              SET Name = @Name,
                  Country = @Country,
                  RegionGroup = @RegionGroup,
                  SortOrder = @SortOrder,
                  ModifiedAt = @Now
              WHERE Id = @Id",
            new
            {
                Id = id,
                request.Name,
                request.Country,
                request.RegionGroup,
                request.SortOrder,
                Now = DateTime.UtcNow
            });

        if (updated == 0)
        {
            throw new NotFoundException("Car brand not found.");
        }
    }

    public void Delete(int id)
    {
        using var conn = _factory.CreateConnection();
        var linkedCarModels = conn.ExecuteScalar<int>(
            "SELECT COUNT(1) FROM CarModels WHERE CarBrandId = @Id",
            new { Id = id });
        if (linkedCarModels > 0)
        {
            throw new ValidationException("Car brand cannot be deleted while car models still reference it.");
        }

        var deleted = conn.Execute("DELETE FROM CarBrands WHERE Id = @Id", new { Id = id });
        if (deleted == 0)
        {
            throw new NotFoundException("Car brand not found.");
        }
    }
}
