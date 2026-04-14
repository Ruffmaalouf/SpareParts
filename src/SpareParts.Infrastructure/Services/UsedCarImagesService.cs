using Dapper;
using SpareParts.Domain.Cars;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Infrastructure.Services;

public sealed class UsedCarImagesService
{
    private const int MaxImageBytes = 8 * 1024 * 1024;
    private readonly ISqlConnectionFactory _factory;

    public UsedCarImagesService(ISqlConnectionFactory factory)
    {
        _factory = factory;
    }

    public IEnumerable<UsedCarImageDto> GetAll(int usedCarId)
    {
        if (usedCarId <= 0)
        {
            throw new ValidationException("Used car is required.");
        }

        using var conn = _factory.CreateConnection();
        EnsureUsedCarExists(conn, usedCarId);

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
        EnsureUsedCarExists(conn, usedCarId);

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
            "DELETE FROM dbo.usedcar_images WHERE ImageId = @ImageId;",
            new { ImageId = imageId });

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

    private static void EnsureUsedCarExists(System.Data.IDbConnection conn, int usedCarId)
    {
        var exists = conn.ExecuteScalar<int>(
            "SELECT COUNT(1) FROM dbo.UsedCars WHERE Id = @UsedCarId;",
            new { UsedCarId = usedCarId });

        if (exists == 0)
        {
            throw new NotFoundException("Used car not found.");
        }
    }
}
