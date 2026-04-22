using Dapper;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Api.Infrastructure;

public static class PartUsedCarMigration
{
    public static void EnsureApplied(ISqlConnectionFactory factory)
    {
        using var conn = factory.CreateConnection();
        conn.Execute(
            @"
IF OBJECT_ID('dbo.Parts', 'U') IS NOT NULL
   AND COL_LENGTH('dbo.Parts', 'UsedCarId') IS NULL
BEGIN
    ALTER TABLE dbo.Parts
        ADD UsedCarId INT NULL;
END;

IF OBJECT_ID('dbo.UsedCarParts', 'U') IS NOT NULL
   AND OBJECT_ID('dbo.Parts', 'U') IS NOT NULL
   AND COL_LENGTH('dbo.Parts', 'UsedCarId') IS NOT NULL
BEGIN
    ;WITH RankedUsedCarParts AS
    (
        SELECT PartId,
               UsedCarId,
               ROW_NUMBER() OVER (PARTITION BY PartId ORDER BY UsedCarId) AS RowNumber
        FROM dbo.UsedCarParts
    )
    UPDATE p
    SET UsedCarId = ranked.UsedCarId
    FROM dbo.Parts p
    INNER JOIN RankedUsedCarParts ranked
        ON ranked.PartId = p.Id
       AND ranked.RowNumber = 1
    WHERE p.UsedCarId IS NULL;

    DROP TABLE dbo.UsedCarParts;
END;

IF OBJECT_ID('dbo.Parts', 'U') IS NOT NULL
   AND COL_LENGTH('dbo.Parts', 'UsedCarId') IS NOT NULL
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM sys.foreign_keys
        WHERE name = 'FK_Parts_UsedCars'
          AND parent_object_id = OBJECT_ID('dbo.Parts'))
       AND OBJECT_ID('dbo.UsedCars', 'U') IS NOT NULL
    BEGIN
        ALTER TABLE dbo.Parts WITH NOCHECK
            ADD CONSTRAINT FK_Parts_UsedCars FOREIGN KEY (UsedCarId) REFERENCES dbo.UsedCars (Id);
    END;

    IF NOT EXISTS (
        SELECT 1
        FROM sys.indexes
        WHERE name = 'IX_Parts_UsedCarId'
          AND object_id = OBJECT_ID('dbo.Parts'))
    BEGIN
        CREATE INDEX IX_Parts_UsedCarId ON dbo.Parts (UsedCarId);
    END;
END;");
    }
}
