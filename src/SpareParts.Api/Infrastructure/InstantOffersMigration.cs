using Dapper;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Api.Infrastructure;

public static class InstantOffersMigration
{
    public static void EnsureApplied(ISqlConnectionFactory factory)
    {
        using var conn = factory.CreateConnection();
        conn.Execute("""
IF OBJECT_ID('dbo.InstantOffers', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.InstantOffers
    (
        Id                 INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_InstantOffers PRIMARY KEY,
        TenantId           INT NOT NULL CONSTRAINT DF_InstantOffers_TenantId DEFAULT 0,
        PartName           NVARCHAR(200) NOT NULL,
        PartDescription    NVARCHAR(1000) NULL,
        PhotoUrls          NVARCHAR(2000) NULL,
        ConditionGrade     NVARCHAR(5) NULL,
        EstimatedValue     DECIMAL(18,4) NOT NULL,
        OfferedPrice       DECIMAL(18,4) NOT NULL,
        Currency           NVARCHAR(10) NOT NULL CONSTRAINT DF_InstantOffers_Currency DEFAULT 'USD',
        ContactName        NVARCHAR(200) NOT NULL,
        ContactPhone       NVARCHAR(50) NULL,
        Status             TINYINT NOT NULL CONSTRAINT DF_InstantOffers_Status DEFAULT 1,
        CreatedAt          DATETIME2 NOT NULL CONSTRAINT DF_InstantOffers_CreatedAt DEFAULT SYSUTCDATETIME(),
        ModifiedAt         DATETIME2 NULL
    );
    CREATE INDEX IX_InstantOffers_TenantId ON dbo.InstantOffers (TenantId);
END;
""");
    }
}
