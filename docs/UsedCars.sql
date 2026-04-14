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
