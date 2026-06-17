using Dapper;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Api.Infrastructure;

public static class PartPassportPhotosMigration
{
    public static void EnsureApplied(ISqlConnectionFactory factory)
    {
        using var conn = factory.CreateConnection();
        conn.Execute("""
IF OBJECT_ID('dbo.PartPassportPhotos', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.PartPassportPhotos
    (
        Id                 INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PartPassportPhotos PRIMARY KEY,
        PartId             INT NOT NULL,
        TransactionId      INT NULL,
        PhotoType          TINYINT NOT NULL,
        PhotoUrl           NVARCHAR(1000) NOT NULL,
        Notes              NVARCHAR(500) NULL,
        CreatedAt          DATETIME2 NOT NULL CONSTRAINT DF_PartPassportPhotos_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedByUserId    INT NULL
    );
    CREATE INDEX IX_PartPassportPhotos_PartId ON dbo.PartPassportPhotos (PartId);
END;
""");
    }
}
