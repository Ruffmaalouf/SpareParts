using Dapper;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Api.Infrastructure;

public static class PartUsedCarStockMigration
{
    public static void EnsureApplied(ISqlConnectionFactory factory)
    {
        using var conn = factory.CreateConnection();
        conn.Execute(
            @"
IF OBJECT_ID('dbo.Parts', 'U') IS NOT NULL
   AND OBJECT_ID('dbo.UsedCars', 'U') IS NOT NULL
   AND OBJECT_ID('dbo.Stock', 'U') IS NOT NULL
   AND OBJECT_ID('dbo.StockMovements', 'U') IS NOT NULL
   AND OBJECT_ID('dbo.Warehouses', 'U') IS NOT NULL
   AND COL_LENGTH('dbo.Parts', 'UsedCarId') IS NOT NULL
BEGIN
    DECLARE @UsedCarPartWarehouseId INT;
    DECLARE @UsedCarPartStockLocationId INT;

    SELECT TOP (1) @UsedCarPartWarehouseId = Id
    FROM dbo.Warehouses
    ORDER BY CASE WHEN IsMain = 1 THEN 0 ELSE 1 END, Id;

    IF @UsedCarPartWarehouseId IS NOT NULL
    BEGIN
        IF OBJECT_ID('dbo.Locations', 'U') IS NOT NULL
        BEGIN
            SELECT TOP (1) @UsedCarPartStockLocationId = Id
            FROM dbo.Locations
            WHERE WarehouseId = @UsedCarPartWarehouseId
            ORDER BY Id;
        END;

        DECLARE @UsedCarPartStockSeed TABLE
        (
            PartId INT NOT NULL PRIMARY KEY,
            UsedCarId INT NOT NULL,
            QuantityToAdd INT NOT NULL,
            UnitCost DECIMAL(18, 2) NOT NULL
        );

        INSERT INTO @UsedCarPartStockSeed
            (PartId, UsedCarId, QuantityToAdd, UnitCost)
        SELECT p.Id,
               p.UsedCarId,
               1 - stock.CurrentQuantity,
               ISNULL(p.CostPrice, 0)
        FROM dbo.Parts p
        INNER JOIN dbo.UsedCars uc ON uc.Id = p.UsedCarId
        OUTER APPLY
        (
            SELECT CurrentQuantity = ISNULL(SUM(s.Quantity), 0)
            FROM dbo.Stock s
            WHERE s.PartId = p.Id
        ) stock
        WHERE p.UsedCarId IS NOT NULL
          AND stock.CurrentQuantity < 1
          AND NOT EXISTS
          (
              SELECT 1
              FROM dbo.StockMovements sm
              WHERE sm.PartId = p.Id
                AND sm.Quantity < 0
                AND (
                    UPPER(LTRIM(RTRIM(CONVERT(NVARCHAR(50), sm.MovementType)))) = N'SALE'
                    OR TRY_CONVERT(INT, CONVERT(NVARCHAR(50), sm.MovementType)) = 2
                )
          );

        UPDATE existingStock
        SET Quantity = existingStock.Quantity + seed.QuantityToAdd,
            ModifiedAt = SYSUTCDATETIME()
        FROM dbo.Stock existingStock
        INNER JOIN @UsedCarPartStockSeed seed
            ON seed.PartId = existingStock.PartId
        WHERE existingStock.WarehouseId = @UsedCarPartWarehouseId;

        INSERT INTO dbo.Stock
            (PartId, WarehouseId, LocationId, Quantity, ReservedQuantity, CreatedAt, CreatedByUserId)
        SELECT seed.PartId,
               @UsedCarPartWarehouseId,
               @UsedCarPartStockLocationId,
               seed.QuantityToAdd,
               0,
               SYSUTCDATETIME(),
               NULL
        FROM @UsedCarPartStockSeed seed
        WHERE NOT EXISTS
        (
            SELECT 1
            FROM dbo.Stock existingStock
            WHERE existingStock.PartId = seed.PartId
              AND existingStock.WarehouseId = @UsedCarPartWarehouseId
        );

        INSERT INTO dbo.StockMovements
            (PartId, WarehouseId, Quantity, MovementType, ReferenceType, ReferenceId, UnitCost, CreatedAt, CreatedByUserId)
        SELECT seed.PartId,
               @UsedCarPartWarehouseId,
               seed.QuantityToAdd,
               N'Adjust',
               N'UsedCar',
               seed.UsedCarId,
               seed.UnitCost,
               SYSUTCDATETIME(),
               NULL
        FROM @UsedCarPartStockSeed seed;
    END;
END;");
    }
}
