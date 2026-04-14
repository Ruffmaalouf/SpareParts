IF OBJECT_ID('dbo.CarModels', 'U') IS NOT NULL
   AND COL_LENGTH('dbo.CarModels', 'BodyType') IS NULL
BEGIN
    ALTER TABLE dbo.CarModels ADD BodyType NVARCHAR(60) NULL;
END;
