using Dapper;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Api.Infrastructure;

public static class UsedCarsMigration
{
    public static void EnsureApplied(ISqlConnectionFactory factory)
    {
        using var conn = factory.CreateConnection();
        conn.Execute(
            @"
IF OBJECT_ID('dbo.UsedCars', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.UsedCars
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_UsedCars PRIMARY KEY,
        CarModelId INT NOT NULL,
        ModelYear INT NOT NULL,
        PriceCurrency CHAR(3) NOT NULL,
        Price DECIMAL(18, 2) NOT NULL CONSTRAINT CK_UsedCars_Price_Positive CHECK (Price > 0),
        PriceBase DECIMAL(18, 2) NOT NULL CONSTRAINT CK_UsedCars_PriceBase_NonNegative CHECK (PriceBase >= 0),
        PriceCounter DECIMAL(18, 2) NOT NULL CONSTRAINT CK_UsedCars_PriceCounter_NonNegative CHECK (PriceCounter >= 0),
        LocationId INT NULL,
        Location NVARCHAR(160) NOT NULL CONSTRAINT DF_UsedCars_Location DEFAULT (N''),
        Transportation DECIMAL(18, 2) NOT NULL CONSTRAINT DF_UsedCars_Transportation DEFAULT (0),
        IsReceived BIT NOT NULL CONSTRAINT DF_UsedCars_IsReceived DEFAULT (0),
        IsShipped BIT NOT NULL CONSTRAINT DF_UsedCars_IsShipped DEFAULT (0),
        PartOut NVARCHAR(160) NOT NULL CONSTRAINT DF_UsedCars_PartOut DEFAULT (N''),
        Shipping DECIMAL(18, 2) NOT NULL CONSTRAINT DF_UsedCars_Shipping DEFAULT (0),
        Customs DECIMAL(18, 2) NOT NULL CONSTRAINT DF_UsedCars_Customs DEFAULT (0),
        TotalBeforeShipping DECIMAL(18, 2) NOT NULL CONSTRAINT DF_UsedCars_TotalBeforeShipping DEFAULT (0),
        GrandTotalBase DECIMAL(18, 2) NOT NULL CONSTRAINT DF_UsedCars_GrandTotalBase DEFAULT (0),
        GrandTotalCounter DECIMAL(18, 2) NOT NULL CONSTRAINT DF_UsedCars_GrandTotalCounter DEFAULT (0),
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_UsedCars_CreatedAt DEFAULT SYSUTCDATETIME(),
        ModifiedAt DATETIME2(0) NULL,
        CreatedByUserId INT NULL,
        ModifiedByUserId INT NULL,
        CONSTRAINT FK_UsedCars_CarModels FOREIGN KEY (CarModelId) REFERENCES dbo.CarModels (Id),
        CONSTRAINT FK_UsedCars_Location FOREIGN KEY (LocationId) REFERENCES dbo.Location (LocationId),
        CONSTRAINT FK_UsedCars_CreatedByUsers FOREIGN KEY (CreatedByUserId) REFERENCES dbo.Users (Id),
        CONSTRAINT FK_UsedCars_ModifiedByUsers FOREIGN KEY (ModifiedByUserId) REFERENCES dbo.Users (Id)
    );

    CREATE INDEX IX_UsedCars_CarModelId ON dbo.UsedCars (CarModelId);
    CREATE INDEX IX_UsedCars_LocationId ON dbo.UsedCars (LocationId);
END;

IF COL_LENGTH('dbo.UsedCars', 'LocationId') IS NULL
BEGIN
    ALTER TABLE dbo.UsedCars ADD LocationId INT NULL;
END;

IF COL_LENGTH('dbo.UsedCars', 'IsReceived') IS NULL
BEGIN
    ALTER TABLE dbo.UsedCars ADD IsReceived BIT NULL;

    UPDATE dbo.UsedCars
    SET IsReceived = CASE WHEN ISNULL(Customs, 0) > 0 THEN 1 ELSE 0 END
    WHERE IsReceived IS NULL;

    ALTER TABLE dbo.UsedCars ALTER COLUMN IsReceived BIT NOT NULL;
END;

IF COL_LENGTH('dbo.UsedCars', 'IsShipped') IS NULL
BEGIN
    ALTER TABLE dbo.UsedCars ADD IsShipped BIT NULL;

    UPDATE dbo.UsedCars
    SET IsShipped = 0
    WHERE IsShipped IS NULL;

    ALTER TABLE dbo.UsedCars ALTER COLUMN IsShipped BIT NOT NULL;
END;

IF OBJECT_ID('dbo.Location', 'U') IS NOT NULL
BEGIN
    UPDATE uc
    SET LocationId = loc.LocationID
    FROM dbo.UsedCars uc
    INNER JOIN dbo.Location loc
        ON UPPER(LTRIM(RTRIM(loc.Name))) = UPPER(LTRIM(RTRIM(uc.Location)))
    WHERE uc.LocationId IS NULL
      AND NULLIF(LTRIM(RTRIM(uc.Location)), N'') IS NOT NULL;

    UPDATE uc
    SET Location = loc.Name
    FROM dbo.UsedCars uc
    INNER JOIN dbo.Location loc ON loc.LocationID = uc.LocationId
    WHERE uc.LocationId IS NOT NULL
      AND (uc.Location IS NULL OR LTRIM(RTRIM(uc.Location)) = N'');
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = 'FK_UsedCars_Location'
      AND parent_object_id = OBJECT_ID('dbo.UsedCars'))
   AND OBJECT_ID('dbo.Location', 'U') IS NOT NULL
BEGIN
    ALTER TABLE dbo.UsedCars WITH NOCHECK
        ADD CONSTRAINT FK_UsedCars_Location FOREIGN KEY (LocationId) REFERENCES dbo.Location (LocationId);
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_UsedCars_LocationId'
      AND object_id = OBJECT_ID('dbo.UsedCars'))
BEGIN
    CREATE INDEX IX_UsedCars_LocationId ON dbo.UsedCars (LocationId);
END;");
    }
}
