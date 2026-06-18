using Dapper;
using SpareParts.Domain.Cars;
using SpareParts.Infrastructure.Data;
using SpareParts.Infrastructure.Interfaces;

namespace SpareParts.Infrastructure.Services;

public sealed class UsedCarImagesService
{
    private const int MaxImageBytes = 8 * 1024 * 1024;
    private readonly ISqlConnectionFactory _factory;
    private readonly ITenantContext _tenantContext;

    public UsedCarImagesService(ISqlConnectionFactory factory, ITenantContext tenantContext)
    {
        _factory = factory;
        _tenantContext = tenantContext;
    }

    public IEnumerable<UsedCarImageDto> GetAll(int usedCarId)
    {
        if (usedCarId <= 0)
        {
            throw new ValidationException("Used car is required.");
        }

        using var conn = _factory.CreateConnection();
        EnsureUsedCarExists(conn, usedCarId, _tenantContext.TenantId);

        return conn.Query<UsedCarImageDto>(
            @"SELECT ImageId AS Id,
                     UsedCarId,
                     ImageData,
                     ImageMimeType AS MimeType,
                     CreatedAt
              FROM dbo.usedcar_images
              WHERE UsedCarId = @UsedCarId
              ORDER BY CreatedAt DESC, ImageId DESC;",
            new { UsedCarId = usedCarId });
    }

    public int Create(int usedCarId, byte[] imageData, string mimeType, int userId)
    {
        ValidateImage(imageData, mimeType);

        using var conn = _factory.CreateConnection();
        EnsureUsedCarExists(conn, usedCarId, _tenantContext.TenantId);

        return conn.ExecuteScalar<int>(
            @"INSERT INTO dbo.usedcar_images
                (UsedCarId, ImageData, ImageMimeType, CreatedByUserId)
              VALUES
                (@UsedCarId, @ImageData, @MimeType, @UserId);
              SELECT CAST(SCOPE_IDENTITY() AS INT);",
            new
            {
                UsedCarId = usedCarId,
                ImageData = imageData,
                MimeType = mimeType.Trim(),
                UserId = userId
            });
    }

    public void Delete(int imageId)
    {
        using var conn = _factory.CreateConnection();
        var deleted = conn.Execute(
            @"DELETE i
              FROM dbo.usedcar_images i
              INNER JOIN dbo.UsedCars uc ON uc.Id = i.UsedCarId
              WHERE i.ImageId = @ImageId AND (@TenantId = 0 OR uc.TenantId = @TenantId);",
            new { ImageId = imageId, TenantId = _tenantContext.TenantId });

        if (deleted == 0)
        {
            throw new NotFoundException("Used car image not found.");
        }
    }

    private static void ValidateImage(byte[] imageData, string mimeType)
    {
        if (imageData.Length == 0)
        {
            throw new ValidationException("Image is required.");
        }

        if (imageData.Length > MaxImageBytes)
        {
            throw new ValidationException("Image must be 8 MB or smaller.");
        }

        if (string.IsNullOrWhiteSpace(mimeType) || !mimeType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            throw new ValidationException("Only image files are allowed.");
        }
    }

    private static void EnsureUsedCarExists(System.Data.IDbConnection conn, int usedCarId, int tenantId)
    {
        var exists = conn.ExecuteScalar<int>(
            "SELECT COUNT(1) FROM dbo.UsedCars WHERE Id = @UsedCarId AND (@TenantId = 0 OR TenantId = @TenantId);",
            new { UsedCarId = usedCarId, TenantId = tenantId });

        if (exists == 0)
        {
            throw new NotFoundException("Used car not found.");
        }
    }
}
