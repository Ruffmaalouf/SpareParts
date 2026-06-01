using Dapper;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Api.Infrastructure;

public static class UsedCarPartPricingMigration
{
    public static void EnsureApplied(ISqlConnectionFactory factory)
    {
        using var conn = factory.CreateConnection();
        conn.Execute(
            """
IF OBJECT_ID('dbo.UsedCars', 'U') IS NOT NULL
   AND COL_LENGTH('dbo.UsedCars', 'ExpectedSellThroughRate') IS NULL
BEGIN
    ALTER TABLE dbo.UsedCars
        ADD ExpectedSellThroughRate DECIMAL(5, 4) NOT NULL
            CONSTRAINT DF_UsedCars_ExpectedSellThroughRate DEFAULT (0.8000);
END;

IF OBJECT_ID('dbo.UsedCars', 'U') IS NOT NULL
BEGIN
    UPDATE dbo.UsedCars
    SET ExpectedSellThroughRate = 0.8000
    WHERE ExpectedSellThroughRate IS NULL
       OR ExpectedSellThroughRate <= 0
       OR ExpectedSellThroughRate > 1;

    IF NOT EXISTS (
        SELECT 1
        FROM sys.check_constraints
        WHERE name = 'CK_UsedCars_ExpectedSellThroughRate'
          AND parent_object_id = OBJECT_ID('dbo.UsedCars'))
    BEGIN
        ALTER TABLE dbo.UsedCars WITH NOCHECK
            ADD CONSTRAINT CK_UsedCars_ExpectedSellThroughRate
            CHECK (ExpectedSellThroughRate > 0 AND ExpectedSellThroughRate <= 1);
    END;
END;
""");

        conn.Execute(
            """
IF OBJECT_ID('dbo.UsedCars', 'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.UsedCars', 'ExpectedSellThroughRate') IS NULL
    BEGIN
        ALTER TABLE dbo.UsedCars
        ADD ExpectedSellThroughRate DECIMAL(5,4) NOT NULL
            CONSTRAINT DF_UsedCars_ExpectedSellThroughRate DEFAULT (0.8000);
    END;
END;

IF OBJECT_ID('dbo.Parts', 'U') IS NOT NULL
BEGIN

    IF COL_LENGTH('dbo.Parts', 'EstimatedMarketPrice') IS NULL
    BEGIN
        ALTER TABLE dbo.Parts
        ADD EstimatedMarketPrice DECIMAL(19,4) NULL;
    END;

    IF COL_LENGTH('dbo.Parts', 'CostAllocationPercent') IS NULL
    BEGIN
        ALTER TABLE dbo.Parts
        ADD CostAllocationPercent DECIMAL(9,4) NOT NULL
            CONSTRAINT DF_Parts_CostAllocationPercent DEFAULT (0);
    END;

    IF COL_LENGTH('dbo.Parts', 'AllocatedCost') IS NULL
    BEGIN
        ALTER TABLE dbo.Parts
        ADD AllocatedCost DECIMAL(19,4) NOT NULL
            CONSTRAINT DF_Parts_AllocatedCost DEFAULT (0);
    END;

    IF COL_LENGTH('dbo.Parts', 'MinimumSellPrice') IS NULL
    BEGIN
        ALTER TABLE dbo.Parts
        ADD MinimumSellPrice DECIMAL(19,4) NOT NULL
            CONSTRAINT DF_Parts_MinimumSellPrice DEFAULT (0);
    END;

    IF COL_LENGTH('dbo.Parts', 'FastSalePrice') IS NULL
    BEGIN
        ALTER TABLE dbo.Parts
        ADD FastSalePrice DECIMAL(19,4) NOT NULL
            CONSTRAINT DF_Parts_FastSalePrice DEFAULT (0);
    END;

    IF COL_LENGTH('dbo.Parts', 'WholesalePrice') IS NULL
    BEGIN
        ALTER TABLE dbo.Parts
        ADD WholesalePrice DECIMAL(19,4) NOT NULL
            CONSTRAINT DF_Parts_WholesalePrice DEFAULT (0);
    END;

    IF COL_LENGTH('dbo.Parts', 'RecommendedPrice') IS NULL
    BEGIN
        ALTER TABLE dbo.Parts
        ADD RecommendedPrice DECIMAL(19,4) NOT NULL
            CONSTRAINT DF_Parts_RecommendedPrice DEFAULT (0);
    END;

    IF COL_LENGTH('dbo.Parts', 'PricingStatus') IS NULL
    BEGIN
        ALTER TABLE dbo.Parts
        ADD PricingStatus NVARCHAR(40) NOT NULL
            CONSTRAINT DF_Parts_PricingStatus DEFAULT (N'Manual');
    END;

    IF COL_LENGTH('dbo.Parts', 'PricingCalculatedAt') IS NULL
    BEGIN
        ALTER TABLE dbo.Parts
        ADD PricingCalculatedAt DATETIME2(0) NULL;
    END;

    UPDATE dbo.Parts
    SET CostAllocationPercent = ISNULL(CostAllocationPercent, 0),
        AllocatedCost = ISNULL(AllocatedCost, 0),
        MinimumSellPrice = ISNULL(MinimumSellPrice, 0),
        FastSalePrice = ISNULL(FastSalePrice, 0),
        WholesalePrice = ISNULL(WholesalePrice, 0),
        RecommendedPrice = ISNULL(RecommendedPrice, 0),
        PricingStatus = COALESCE(NULLIF(LTRIM(RTRIM(PricingStatus)), N''), N'Manual');

END;
""");

        conn.Execute(
            """
IF OBJECT_ID('dbo.Parts', 'U') IS NOT NULL
   AND OBJECT_ID('dbo.UsedCars', 'U') IS NOT NULL
   AND COL_LENGTH('dbo.Parts', 'UsedCarId') IS NOT NULL
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM sys.foreign_keys
        WHERE name = 'FK_Parts_UsedCars'
          AND parent_object_id = OBJECT_ID('dbo.Parts'))
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

    ;WITH Seeds AS
    (
        SELECT
            p.Id,
            p.SalePrice AS CurrentSalePrice,
            expected.ExpectedMarketPrice,
            rate.PartRateToBase,
            ExpectedMarketPriceBase = expected.ExpectedMarketPrice * rate.PartRateToBase,
            EffectiveVehicleCostBase = ROUND(ISNULL(uc.GrandTotalBase, 0) /
                CASE
                    WHEN ISNULL(uc.ExpectedSellThroughRate, 0) > 0
                     AND ISNULL(uc.ExpectedSellThroughRate, 0) <= 1
                    THEN uc.ExpectedSellThroughRate
                    ELSE 0.8000
                END, 2),
            TotalExpectedRevenueBase = SUM(expected.ExpectedMarketPrice * rate.PartRateToBase) OVER (PARTITION BY p.UsedCarId)
        FROM dbo.Parts p
        INNER JOIN dbo.UsedCars uc ON uc.Id = p.UsedCarId
        CROSS APPLY
        (
            SELECT
                BaseCurrencyCode = COALESCE(NULLIF(UPPER(LTRIM(RTRIM(uc.BaseCurrencyCode))), N''), N'USD'),
                CounterCurrencyCode = COALESCE(NULLIF(UPPER(LTRIM(RTRIM(uc.CounterCurrencyCode))), N''), COALESCE(NULLIF(UPPER(LTRIM(RTRIM(uc.BaseCurrencyCode))), N''), N'USD')),
                PartCurrencyCode = COALESCE(NULLIF(UPPER(LTRIM(RTRIM(p.Currency))), N''), COALESCE(NULLIF(UPPER(LTRIM(RTRIM(uc.CounterCurrencyCode))), N''), COALESCE(NULLIF(UPPER(LTRIM(RTRIM(uc.BaseCurrencyCode))), N''), N'USD'))),
                CounterRateToBase = CASE WHEN ISNULL(uc.CounterRateToBase, 0) > 0 THEN uc.CounterRateToBase ELSE 1 END
        ) currency
        LEFT JOIN dbo.CurrencyRates partRate ON UPPER(LTRIM(RTRIM(partRate.Code))) = currency.PartCurrencyCode
        LEFT JOIN dbo.CurrencyRates baseRate ON UPPER(LTRIM(RTRIM(baseRate.Code))) = currency.BaseCurrencyCode
        CROSS APPLY
        (
            SELECT PartRateToBase = CASE
                WHEN currency.PartCurrencyCode = currency.BaseCurrencyCode THEN 1.0
                WHEN currency.PartCurrencyCode = currency.CounterCurrencyCode THEN currency.CounterRateToBase
                WHEN partRate.RateToUsd > 0
                 AND UPPER(LTRIM(RTRIM(partRate.BaseCode))) = currency.BaseCurrencyCode THEN 1.0 / partRate.RateToUsd
                WHEN baseRate.RateToUsd > 0
                 AND UPPER(LTRIM(RTRIM(baseRate.BaseCode))) = currency.PartCurrencyCode THEN baseRate.RateToUsd
                WHEN partRate.RateToUsd > 0
                 AND baseRate.RateToUsd > 0
                 AND UPPER(LTRIM(RTRIM(partRate.BaseCode))) = UPPER(LTRIM(RTRIM(baseRate.BaseCode))) THEN baseRate.RateToUsd / partRate.RateToUsd
                ELSE 1.0
            END
        ) rate
        CROSS APPLY
        (
            SELECT ExpectedMarketPrice = COALESCE(
                NULLIF(p.EstimatedMarketPrice, 0),
                NULLIF(p.AveragePrice, 0),
                NULLIF(p.SalePrice, 0),
                0)
        ) expected
        WHERE p.UsedCarId IS NOT NULL
          AND ISNULL(p.IsActive, 1) = 1
    )
    UPDATE p
    SET EstimatedMarketPrice = CASE WHEN s.ExpectedMarketPrice > 0 THEN s.ExpectedMarketPrice ELSE p.EstimatedMarketPrice END,
        CostAllocationPercent = priced.CostAllocationPercent,
        AllocatedCost = priced.AllocatedCost,
        MinimumSellPrice = priced.AllocatedCost,
        FastSalePrice = priced.FastSalePrice,
        WholesalePrice = priced.WholesalePrice,
        RecommendedPrice = priced.RecommendedPrice,
        PricingStatus = CASE
            WHEN s.ExpectedMarketPrice <= 0 OR s.TotalExpectedRevenueBase <= 0 THEN N'NeedsExpectedPrice'
            WHEN s.EffectiveVehicleCostBase <= 0 THEN N'NeedsVehicleCost'
            ELSE N'Allocated'
        END,
        PricingCalculatedAt = SYSUTCDATETIME(),
        CostPrice = CASE WHEN priced.AllocatedCost > 0 THEN priced.AllocatedCost ELSE p.CostPrice END,
        SalePrice = CASE WHEN ISNULL(p.SalePrice, 0) <= 0 AND priced.RecommendedPrice > 0 THEN priced.RecommendedPrice ELSE p.SalePrice END
    FROM dbo.Parts p
    INNER JOIN Seeds s ON s.Id = p.Id
    CROSS APPLY
    (
        SELECT AllocationRatio = CASE
            WHEN s.TotalExpectedRevenueBase > 0 AND s.ExpectedMarketPriceBase > 0 THEN s.ExpectedMarketPriceBase / s.TotalExpectedRevenueBase
            ELSE 0
        END
    ) ratio
    CROSS APPLY
    (
        SELECT AllocatedCost = ROUND((s.EffectiveVehicleCostBase * ratio.AllocationRatio) / CASE WHEN s.PartRateToBase > 0 THEN s.PartRateToBase ELSE 1 END, 2)
    ) allocated
    CROSS APPLY
    (
        SELECT WholesalePrice = ROUND(allocated.AllocatedCost * 1.2000, 2)
    ) wholesale
    CROSS APPLY
    (
        SELECT
            CostAllocationPercent = ROUND(ratio.AllocationRatio * 100, 4),
            AllocatedCost = allocated.AllocatedCost,
            FastSalePrice = ROUND(allocated.AllocatedCost * 1.1000, 2),
            WholesalePrice = wholesale.WholesalePrice,
            RecommendedPrice = CASE
                WHEN s.ExpectedMarketPrice > wholesale.WholesalePrice THEN ROUND(s.ExpectedMarketPrice, 2)
                ELSE wholesale.WholesalePrice
            END
    ) priced
    WHERE ISNULL(p.PricingStatus, N'') IN (N'', N'Manual', N'Allocated', N'NeedsExpectedPrice', N'NeedsVehicleCost');
END;
""");
    }
}
