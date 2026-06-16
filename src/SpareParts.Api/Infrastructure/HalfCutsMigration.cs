using Dapper;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Api.Infrastructure;

public static class HalfCutsMigration
{
    public static void EnsureApplied(ISqlConnectionFactory factory)
    {
        using var conn = factory.CreateConnection();
        conn.Execute("""
IF OBJECT_ID('dbo.HalfCutListings', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.HalfCutListings
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_HalfCutListings PRIMARY KEY,
        TenantId INT NOT NULL CONSTRAINT DF_HalfCutListings_TenantId DEFAULT 0,
        VehicleMake NVARCHAR(100) NOT NULL,
        VehicleModel NVARCHAR(100) NOT NULL,
        VehicleYear INT NOT NULL,
        VinNumber NVARCHAR(17) NULL,
        Color NVARCHAR(60) NULL,
        OriginCountry NVARCHAR(100) NULL,
        Mileage INT NULL,
        Notes NVARCHAR(1000) NULL,
        PhotoUrls NVARCHAR(2000) NULL,
        AskingPrice DECIMAL(18,2) NULL,
        Currency NVARCHAR(10) NOT NULL CONSTRAINT DF_HalfCutListings_Currency DEFAULT 'USD',
        Status INT NOT NULL CONSTRAINT DF_HalfCutListings_Status DEFAULT 1,
        ClaimsCount INT NOT NULL CONSTRAINT DF_HalfCutListings_ClaimsCount DEFAULT 0,
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_HalfCutListings_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedByUserId INT NULL,
        ModifiedAt DATETIME2 NULL,
        ModifiedByUserId INT NULL
    );
    CREATE INDEX IX_HalfCutListings_TenantId ON dbo.HalfCutListings (TenantId);
    CREATE INDEX IX_HalfCutListings_Status ON dbo.HalfCutListings (Status);
END;

IF OBJECT_ID('dbo.HalfCutPartClaims', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.HalfCutPartClaims
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_HalfCutPartClaims PRIMARY KEY,
        HalfCutListingId INT NOT NULL CONSTRAINT FK_HalfCutPartClaims_HalfCutListings FOREIGN KEY REFERENCES dbo.HalfCutListings (Id),
        TenantId INT NOT NULL CONSTRAINT DF_HalfCutPartClaims_TenantId DEFAULT 0,
        ClaimantUserId INT NULL,
        ClaimantName NVARCHAR(120) NOT NULL,
        ClaimantPhone NVARCHAR(40) NULL,
        PartName NVARCHAR(200) NOT NULL,
        Notes NVARCHAR(500) NULL,
        DepositAmount DECIMAL(18,2) NULL,
        Currency NVARCHAR(10) NOT NULL CONSTRAINT DF_HalfCutPartClaims_Currency DEFAULT 'USD',
        IsConfirmed BIT NOT NULL CONSTRAINT DF_HalfCutPartClaims_IsConfirmed DEFAULT 0,
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_HalfCutPartClaims_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedByUserId INT NULL,
        ModifiedAt DATETIME2 NULL,
        ModifiedByUserId INT NULL
    );
    CREATE INDEX IX_HalfCutPartClaims_HalfCutListingId ON dbo.HalfCutPartClaims (HalfCutListingId);
END;
""");
    }
}
