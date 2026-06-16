using Dapper;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Api.Infrastructure;

public static class NeedBoardMigration
{
    public static void EnsureApplied(ISqlConnectionFactory factory)
    {
        using var conn = factory.CreateConnection();
        conn.Execute("""
IF OBJECT_ID('dbo.PartWantedAds', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.PartWantedAds (
        Id                INT              NOT NULL IDENTITY(1,1) CONSTRAINT PK_PartWantedAds PRIMARY KEY,
        TenantId          INT              NOT NULL CONSTRAINT DF_PartWantedAds_TenantId DEFAULT 0,
        BuyerUserId       INT              NULL,
        BuyerName         NVARCHAR(120)    NOT NULL,
        BuyerPhone        NVARCHAR(40)     NULL,
        PartName          NVARCHAR(200)    NOT NULL,
        Notes             NVARCHAR(1000)   NULL,
        VehicleMake       NVARCHAR(100)    NULL,
        VehicleModel      NVARCHAR(100)    NULL,
        VehicleYear       INT              NULL,
        VinNumber         NVARCHAR(17)     NULL,
        Budget            DECIMAL(18,2)    NULL,
        Currency          NVARCHAR(10)     NOT NULL CONSTRAINT DF_PartWantedAds_Currency DEFAULT N'USD',
        LocationCity      NVARCHAR(100)    NULL,
        LocationCountry   NVARCHAR(100)    NULL,
        Urgency           NVARCHAR(20)     NOT NULL CONSTRAINT DF_PartWantedAds_Urgency DEFAULT N'Normal',
        PhotoUrl          NVARCHAR(500)    NULL,
        Status            INT              NOT NULL CONSTRAINT DF_PartWantedAds_Status DEFAULT 1,
        ExpiresAt         DATETIME2        NULL,
        OffersCount       INT              NOT NULL CONSTRAINT DF_PartWantedAds_OffersCount DEFAULT 0,
        CreatedAt         DATETIME2        NOT NULL CONSTRAINT DF_PartWantedAds_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedByUserId   INT              NULL,
        ModifiedAt        DATETIME2        NULL,
        ModifiedByUserId  INT              NULL
    );
    CREATE INDEX IX_PartWantedAds_Status   ON dbo.PartWantedAds (Status);
    CREATE INDEX IX_PartWantedAds_TenantId ON dbo.PartWantedAds (TenantId);
END;

IF OBJECT_ID('dbo.PartWantedAdOffers', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.PartWantedAdOffers (
        Id                INT            NOT NULL IDENTITY(1,1) CONSTRAINT PK_PartWantedAdOffers PRIMARY KEY,
        WantedAdId        INT            NOT NULL CONSTRAINT FK_PartWantedAdOffers_WantedAds FOREIGN KEY REFERENCES dbo.PartWantedAds (Id),
        TenantId          INT            NOT NULL CONSTRAINT DF_PartWantedAdOffers_TenantId DEFAULT 0,
        SellerUserId      INT            NULL,
        SellerName        NVARCHAR(120)  NOT NULL,
        SellerPhone       NVARCHAR(40)   NULL,
        PartId            INT            NULL,
        OfferedPrice      DECIMAL(18,2)  NULL,
        Currency          NVARCHAR(10)   NOT NULL CONSTRAINT DF_PartWantedAdOffers_Currency DEFAULT N'USD',
        Condition         NVARCHAR(60)   NULL,
        Notes             NVARCHAR(1000) NULL,
        PhotoUrl          NVARCHAR(500)  NULL,
        IsAccepted        BIT            NOT NULL CONSTRAINT DF_PartWantedAdOffers_IsAccepted DEFAULT 0,
        CreatedAt         DATETIME2      NOT NULL CONSTRAINT DF_PartWantedAdOffers_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedByUserId   INT            NULL,
        ModifiedAt        DATETIME2      NULL,
        ModifiedByUserId  INT            NULL
    );
    CREATE INDEX IX_PartWantedAdOffers_WantedAdId ON dbo.PartWantedAdOffers (WantedAdId);
END;
""");
    }
}
