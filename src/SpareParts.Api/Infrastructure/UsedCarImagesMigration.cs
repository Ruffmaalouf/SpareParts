using Dapper;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Api.Infrastructure;

public static class UsedCarImagesMigration
{
    public static void EnsureApplied(ISqlConnectionFactory factory)
    {
        using var conn = factory.CreateConnection();
        conn.Execute(
            @"
IF OBJECT_ID('dbo.usedcar_images', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.usedcar_images
    (
        ImageId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_usedcar_images PRIMARY KEY,
        UsedCarId INT NOT NULL,
        ImageData VARBINARY(MAX) NOT NULL,
        ImageMimeType NVARCHAR(100) NOT NULL,
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_usedcar_images_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedByUserId INT NULL,
        CONSTRAINT FK_usedcar_images_UsedCars FOREIGN KEY (UsedCarId) REFERENCES dbo.UsedCars (Id) ON DELETE CASCADE,
        CONSTRAINT FK_usedcar_images_CreatedByUsers FOREIGN KEY (CreatedByUserId) REFERENCES dbo.Users (Id)
    );

    CREATE INDEX IX_usedcar_images_UsedCarId ON dbo.usedcar_images (UsedCarId);
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_usedcar_images_UsedCarId'
      AND object_id = OBJECT_ID('dbo.usedcar_images'))
BEGIN
    CREATE INDEX IX_usedcar_images_UsedCarId ON dbo.usedcar_images (UsedCarId);
END;");
    }
}
