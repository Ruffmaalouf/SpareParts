IF OBJECT_ID('dbo.Location', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Location
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Location PRIMARY KEY,
        Name NVARCHAR(160) NOT NULL,
        ShippingFees DECIMAL(18, 2) NOT NULL CONSTRAINT DF_Location_ShippingFees DEFAULT (0),
        ShippingFeesCurrencyCode CHAR(3) NOT NULL CONSTRAINT DF_Location_ShippingFeesCurrencyCode DEFAULT ('USD'),
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_Location_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedByUserId INT NULL,
        ModifiedAt DATETIME2(0) NULL,
        ModifiedByUserId INT NULL,
        CONSTRAINT FK_Location_CreatedByUsers FOREIGN KEY (CreatedByUserId) REFERENCES dbo.Users (Id),
        CONSTRAINT FK_Location_ModifiedByUsers FOREIGN KEY (ModifiedByUserId) REFERENCES dbo.Users (Id)
    );

    CREATE INDEX IX_Location_Name ON dbo.Location (Name);
END;
