IF COL_LENGTH('dbo.CarModels', 'BasePrice') IS NOT NULL
BEGIN
    DECLARE @BasePriceConstraint sysname;

    SELECT @BasePriceConstraint = dc.name
    FROM sys.default_constraints dc
    INNER JOIN sys.columns c
        ON c.default_object_id = dc.object_id
    INNER JOIN sys.tables t
        ON t.object_id = c.object_id
    INNER JOIN sys.schemas s
        ON s.schema_id = t.schema_id
    WHERE s.name = N'dbo'
      AND t.name = N'CarModels'
      AND c.name = N'BasePrice';

    IF @BasePriceConstraint IS NOT NULL
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[CarModels] DROP CONSTRAINT [' + REPLACE(@BasePriceConstraint, ']', ']]') + N']');
    END;

    ALTER TABLE [dbo].[CarModels] DROP COLUMN [BasePrice];
END;

IF COL_LENGTH('dbo.CarModels', 'Year') IS NOT NULL
BEGIN
    ALTER TABLE [dbo].[CarModels] DROP COLUMN [Year];
END;

IF COL_LENGTH('dbo.CarModels', 'EngineType') IS NOT NULL
BEGIN
    ALTER TABLE [dbo].[CarModels] DROP COLUMN [EngineType];
END;
