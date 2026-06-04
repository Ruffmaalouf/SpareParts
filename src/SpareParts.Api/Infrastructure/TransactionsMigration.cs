using Dapper;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Api.Infrastructure;

public static class TransactionsMigration
{
    public static void EnsureApplied(ISqlConnectionFactory factory)
    {
        using var conn = factory.CreateConnection();

        conn.Execute(
            @"
IF OBJECT_ID('dbo.TransactionItems', 'U') IS NOT NULL
   AND COL_LENGTH('dbo.TransactionItems', 'DetailKey') IS NULL
BEGIN
    ALTER TABLE dbo.TransactionItems ADD DetailKey NVARCHAR(80) NULL;
END;");

        conn.Execute(
            @"
IF COL_LENGTH('dbo.TransactionTypes', 'TypeKey') IS NULL
BEGIN
    ALTER TABLE dbo.TransactionTypes ADD TypeKey NVARCHAR(80) NULL;
END;

IF COL_LENGTH('dbo.TransactionTypes', 'SortOrder') IS NULL
BEGIN
    ALTER TABLE dbo.TransactionTypes ADD SortOrder INT NOT NULL CONSTRAINT DF_TransactionTypes_SortOrder DEFAULT (0);
END;

IF COL_LENGTH('dbo.TransactionTypes', 'SerialNumberFormat') IS NULL
BEGIN
    ALTER TABLE dbo.TransactionTypes ADD SerialNumberFormat NVARCHAR(200) NULL;
END;

IF COL_LENGTH('dbo.TransactionTypes', 'SerialStartNumber') IS NULL
BEGIN
    ALTER TABLE dbo.TransactionTypes ADD SerialStartNumber BIGINT NULL;
END;

IF COL_LENGTH('dbo.TransactionTypes', 'SerialCurrentNumber') IS NULL
BEGIN
    ALTER TABLE dbo.TransactionTypes ADD SerialCurrentNumber BIGINT NULL;
END;

UPDATE dbo.TransactionTypes
SET TypeKey = CASE
        WHEN UPPER(LTRIM(RTRIM(Name))) = 'SALES' THEN 'sale'
        WHEN UPPER(LTRIM(RTRIM(Name))) IN ('PURCHASE', 'PURCHASES') THEN 'purchase'
        WHEN UPPER(REPLACE(REPLACE(REPLACE(LTRIM(RTRIM(Name)), ' ', ''), '-', ''), '_', '')) IN ('USEDCARPURCHASE', 'USEDCARPURCHASES') THEN 'used_car_purchase'
        ELSE CONCAT('type_', Id)
    END
WHERE TypeKey IS NULL
   OR LTRIM(RTRIM(TypeKey)) = '';

UPDATE dbo.TransactionTypes
SET SortOrder = CASE
        WHEN TypeKey = 'sale' THEN 10
        WHEN TypeKey = 'purchase' THEN 20
        WHEN TypeKey = 'used_car_purchase' THEN 30
        ELSE 1000 + Id
    END
WHERE ISNULL(SortOrder, 0) = 0;

UPDATE dbo.TransactionTypes
SET SerialNumberFormat = CASE
        WHEN TypeKey = 'sale' THEN N'INV-{DATE:yyyyMMdd}-{NUMBER:00000000}'
        WHEN TypeKey = 'purchase' THEN N'PUR-{DATE:yyyyMMdd}-{NUMBER:00000000}'
        WHEN TypeKey = 'used_car_purchase' THEN N'PUR-{DATE:yyyyMMdd}-{NUMBER:00000000}'
        ELSE N'TXN-{DATE:yyyyMMdd}-{NUMBER:00000000}'
    END
WHERE SerialNumberFormat IS NULL
   OR LTRIM(RTRIM(SerialNumberFormat)) = '';

UPDATE dbo.TransactionTypes
SET SerialStartNumber = 1
WHERE SerialStartNumber IS NULL
   OR SerialStartNumber <= 0;

UPDATE dbo.TransactionTypes
SET SerialCurrentNumber = 0
WHERE SerialCurrentNumber IS NULL
   OR SerialCurrentNumber < 0;

BEGIN TRY
    ALTER TABLE dbo.TransactionTypes ALTER COLUMN TypeKey NVARCHAR(80) NOT NULL;
END TRY
BEGIN CATCH
END CATCH;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID('dbo.TransactionTypes')
      AND name = 'UX_TransactionTypes_TypeKey'
)
BEGIN
    CREATE UNIQUE INDEX UX_TransactionTypes_TypeKey
        ON dbo.TransactionTypes(TypeKey);
END;

IF NOT EXISTS (SELECT 1 FROM dbo.TransactionTypes WHERE TypeKey = 'sale')
BEGIN
    INSERT INTO dbo.TransactionTypes (TypeKey, Name, CurrencyCode, CounterRate, SerialNumberFormat, SerialStartNumber, SerialCurrentNumber, IsActive, SortOrder)
    VALUES ('sale', 'Sales', 'USD', 1, 'INV-{DATE:yyyyMMdd}-{NUMBER:00000000}', 1, 0, 1, 10);
END;

IF NOT EXISTS (SELECT 1 FROM dbo.TransactionTypes WHERE TypeKey = 'purchase')
BEGIN
    INSERT INTO dbo.TransactionTypes (TypeKey, Name, CurrencyCode, CounterRate, SerialNumberFormat, SerialStartNumber, SerialCurrentNumber, IsActive, SortOrder)
    VALUES ('purchase', 'Purchases', 'USD', 1, 'PUR-{DATE:yyyyMMdd}-{NUMBER:00000000}', 1, 0, 1, 20);
END;

IF NOT EXISTS (SELECT 1 FROM dbo.TransactionTypes WHERE TypeKey = 'used_car_purchase')
BEGIN
    INSERT INTO dbo.TransactionTypes (TypeKey, Name, CurrencyCode, CounterRate, SerialNumberFormat, SerialStartNumber, SerialCurrentNumber, IsActive, SortOrder)
    VALUES ('used_car_purchase', 'Used Car Purchases', 'USD', 1, 'PUR-{DATE:yyyyMMdd}-{NUMBER:00000000}', 1, 0, 1, 30);
END;

IF OBJECT_ID('dbo.Transactions', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Transactions
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Transactions PRIMARY KEY,
        TransactionTypeId INT NOT NULL,
        ReferenceId INT NOT NULL CONSTRAINT DF_Transactions_ReferenceId DEFAULT (0),
        TransactionNumber NVARCHAR(32) NOT NULL,
        TransactionDate DATETIME2(0) NOT NULL,
        CustomerId INT NULL,
        SupplierId INT NULL,
        WarehouseId INT NULL,
        UsedCarId INT NULL,
        Subtotal DECIMAL(19, 4) NOT NULL CONSTRAINT DF_Transactions_Subtotal DEFAULT (0),
        DiscountAmount DECIMAL(19, 4) NOT NULL CONSTRAINT DF_Transactions_DiscountAmount DEFAULT (0),
        TaxAmount DECIMAL(19, 4) NOT NULL CONSTRAINT DF_Transactions_TaxAmount DEFAULT (0),
        TotalAmount DECIMAL(19, 4) NOT NULL CONSTRAINT DF_Transactions_TotalAmount DEFAULT (0),
        PaidAmount DECIMAL(19, 4) NOT NULL CONSTRAINT DF_Transactions_PaidAmount DEFAULT (0),
        PaymentStatus NVARCHAR(20) NOT NULL CONSTRAINT DF_Transactions_PaymentStatus DEFAULT (N'Unpaid'),
        PaymentMethod NVARCHAR(50) NULL,
        Notes NVARCHAR(1000) NULL,
        IsReturn BIT NOT NULL CONSTRAINT DF_Transactions_IsReturn DEFAULT (0),
        ParentReferenceId INT NULL,
        TotalCost DECIMAL(19, 4) NOT NULL CONSTRAINT DF_Transactions_TotalCost DEFAULT (0),
        PostingStatus NVARCHAR(20) NULL,
        PostedAt DATETIME2(0) NULL,
        PostedByUserId INT NULL,
        BaseCurrencyCode CHAR(3) NULL,
        CounterCurrencyCode CHAR(3) NULL,
        TotalBaseAmount DECIMAL(19, 4) NULL,
        TotalCounterAmount DECIMAL(19, 4) NULL,
        PaidCounterAmount DECIMAL(19, 4) NULL,
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_Transactions_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedByUserId INT NULL,
        ModifiedAt DATETIME2(0) NULL,
        ModifiedByUserId INT NULL,
        CONSTRAINT FK_Transactions_TransactionTypes FOREIGN KEY (TransactionTypeId) REFERENCES dbo.TransactionTypes(Id)
    );
END;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID('dbo.Transactions')
      AND name = 'UX_Transactions_Type_ReferenceId'
)
BEGIN
    CREATE UNIQUE INDEX UX_Transactions_Type_ReferenceId
        ON dbo.Transactions(TransactionTypeId, ReferenceId);
END;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID('dbo.Transactions')
      AND name = 'UX_Transactions_Type_Number'
)
BEGIN
    CREATE UNIQUE INDEX UX_Transactions_Type_Number
        ON dbo.Transactions(TransactionTypeId, TransactionNumber);
END;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID('dbo.Transactions')
      AND name = 'IX_Transactions_Type_Date'
)
BEGIN
    CREATE INDEX IX_Transactions_Type_Date
        ON dbo.Transactions(TransactionTypeId, TransactionDate DESC, Id DESC);
END;

IF OBJECT_ID('dbo.TransactionItems', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.TransactionItems
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_TransactionItems PRIMARY KEY,
        TransactionId INT NOT NULL,
        ItemType NVARCHAR(40) NOT NULL,
        PartId INT NULL,
        AccountId INT NULL,
        DetailKey NVARCHAR(80) NULL,
        Description NVARCHAR(160) NULL,
        Quantity DECIMAL(19, 4) NULL,
        UnitPrice DECIMAL(19, 4) NULL,
        UnitCost DECIMAL(19, 4) NULL,
        DiscountAmount DECIMAL(19, 4) NOT NULL CONSTRAINT DF_TransactionItems_DiscountAmount DEFAULT (0),
        TaxRate DECIMAL(19, 4) NOT NULL CONSTRAINT DF_TransactionItems_TaxRate DEFAULT (0),
        Amount DECIMAL(19, 4) NULL,
        LineTotal DECIMAL(19, 4) NOT NULL CONSTRAINT DF_TransactionItems_LineTotal DEFAULT (0),
        CurrencyCode CHAR(3) NULL,
        RateToBase DECIMAL(19, 8) NULL,
        BaseAmount DECIMAL(19, 4) NULL,
        CounterAmount DECIMAL(19, 4) NULL,
        SortOrder INT NOT NULL CONSTRAINT DF_TransactionItems_SortOrder DEFAULT (0),
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_TransactionItems_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedByUserId INT NULL,
        ModifiedAt DATETIME2(0) NULL,
        ModifiedByUserId INT NULL,
        CONSTRAINT FK_TransactionItems_Transactions FOREIGN KEY (TransactionId) REFERENCES dbo.Transactions(Id) ON DELETE CASCADE
    );
END;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID('dbo.TransactionItems')
      AND name = 'IX_TransactionItems_TransactionId'
)
BEGIN
    CREATE INDEX IX_TransactionItems_TransactionId
        ON dbo.TransactionItems(TransactionId, SortOrder, Id);
END;

DECLARE @SaleTransactionTypeId INT;
DECLARE @PurchaseTransactionTypeId INT;
DECLARE @UsedCarPurchaseTransactionTypeId INT;

SELECT @SaleTransactionTypeId = Id FROM dbo.TransactionTypes WHERE TypeKey = 'sale';
SELECT @PurchaseTransactionTypeId = Id FROM dbo.TransactionTypes WHERE TypeKey = 'purchase';
SELECT @UsedCarPurchaseTransactionTypeId = Id FROM dbo.TransactionTypes WHERE TypeKey = 'used_car_purchase';

IF OBJECT_ID('dbo.SalesInvoices', 'U') IS NOT NULL AND @SaleTransactionTypeId IS NOT NULL
BEGIN
    INSERT INTO dbo.Transactions
    (
        TransactionTypeId,
        ReferenceId,
        TransactionNumber,
        TransactionDate,
        CustomerId,
        WarehouseId,
        Subtotal,
        DiscountAmount,
        TaxAmount,
        TotalAmount,
        PaidAmount,
        PaymentStatus,
        PaymentMethod,
        Notes,
        IsReturn,
        ParentReferenceId,
        TotalCost,
        BaseCurrencyCode,
        CounterCurrencyCode,
        TotalBaseAmount,
        TotalCounterAmount,
        PaidCounterAmount,
        CreatedAt,
        CreatedByUserId,
        ModifiedAt,
        ModifiedByUserId
    )
    SELECT
        @SaleTransactionTypeId,
        s.Id,
        s.InvoiceNumber,
        s.InvoiceDate,
        s.CustomerId,
        s.WarehouseId,
        s.Subtotal,
        s.DiscountAmount,
        s.TaxAmount,
        s.TotalAmount,
        s.PaidAmount,
        s.PaymentStatus,
        s.PaymentMethod,
        s.Notes,
        s.IsReturn,
        s.ParentInvoiceId,
        s.TotalCost,
        tt.CurrencyCode,
        tt.CurrencyCode,
        s.TotalAmount,
        s.TotalAmount,
        s.PaidAmount,
        s.CreatedAt,
        s.CreatedByUserId,
        s.ModifiedAt,
        s.ModifiedByUserId
    FROM dbo.SalesInvoices s
    INNER JOIN dbo.TransactionTypes tt ON tt.Id = @SaleTransactionTypeId
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM dbo.Transactions existing
        WHERE existing.TransactionTypeId = @SaleTransactionTypeId
          AND existing.ReferenceId = s.Id
    );

    INSERT INTO dbo.TransactionItems
    (
        TransactionId,
        ItemType,
        PartId,
        Quantity,
        UnitPrice,
        DiscountAmount,
        TaxRate,
        Amount,
        LineTotal,
        CurrencyCode,
        RateToBase,
        BaseAmount,
        CounterAmount,
        SortOrder,
        CreatedAt,
        CreatedByUserId
    )
    SELECT
        t.Id,
        N'sale_item',
        si.PartId,
        CAST(si.Quantity AS DECIMAL(19, 4)),
        si.UnitPrice,
        si.DiscountAmount,
        si.TaxRate,
        CAST(si.Quantity * si.UnitPrice AS DECIMAL(19, 4)),
        si.LineTotal,
        t.CounterCurrencyCode,
        1,
        si.LineTotal,
        si.LineTotal,
        ROW_NUMBER() OVER (PARTITION BY si.InvoiceId ORDER BY si.Id),
        si.CreatedAt,
        si.CreatedByUserId
    FROM dbo.SalesInvoiceItems si
    INNER JOIN dbo.Transactions t
        ON t.TransactionTypeId = @SaleTransactionTypeId
       AND t.ReferenceId = si.InvoiceId
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM dbo.TransactionItems existing
        WHERE existing.TransactionId = t.Id
    );
END;

IF OBJECT_ID('dbo.PurchaseInvoices', 'U') IS NOT NULL AND @PurchaseTransactionTypeId IS NOT NULL
BEGIN
    INSERT INTO dbo.Transactions
    (
        TransactionTypeId,
        ReferenceId,
        TransactionNumber,
        TransactionDate,
        SupplierId,
        WarehouseId,
        Subtotal,
        DiscountAmount,
        TaxAmount,
        TotalAmount,
        PaidAmount,
        PaymentStatus,
        BaseCurrencyCode,
        CounterCurrencyCode,
        TotalBaseAmount,
        TotalCounterAmount,
        PaidCounterAmount,
        CreatedAt,
        CreatedByUserId,
        ModifiedAt,
        ModifiedByUserId
    )
    SELECT
        @PurchaseTransactionTypeId,
        p.Id,
        p.PurchaseNumber,
        p.PurchaseDate,
        p.SupplierId,
        p.WarehouseId,
        p.Subtotal,
        p.DiscountAmount,
        p.TaxAmount,
        p.TotalAmount,
        p.PaidAmount,
        p.PaymentStatus,
        tt.CurrencyCode,
        tt.CurrencyCode,
        p.TotalAmount,
        p.TotalAmount,
        p.PaidAmount,
        p.CreatedAt,
        p.CreatedByUserId,
        p.ModifiedAt,
        p.ModifiedByUserId
    FROM dbo.PurchaseInvoices p
    INNER JOIN dbo.TransactionTypes tt ON tt.Id = @PurchaseTransactionTypeId
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM dbo.Transactions existing
        WHERE existing.TransactionTypeId = @PurchaseTransactionTypeId
          AND existing.ReferenceId = p.Id
    );

    INSERT INTO dbo.TransactionItems
    (
        TransactionId,
        ItemType,
        PartId,
        Quantity,
        UnitCost,
        TaxRate,
        Amount,
        LineTotal,
        CurrencyCode,
        RateToBase,
        BaseAmount,
        CounterAmount,
        SortOrder,
        CreatedAt,
        CreatedByUserId
    )
    SELECT
        t.Id,
        N'purchase_item',
        pi.PartId,
        CAST(pi.Quantity AS DECIMAL(19, 4)),
        pi.UnitCost,
        pi.TaxRate,
        CAST(pi.Quantity * pi.UnitCost AS DECIMAL(19, 4)),
        pi.LineTotal,
        t.CounterCurrencyCode,
        1,
        pi.LineTotal,
        pi.LineTotal,
        ROW_NUMBER() OVER (PARTITION BY pi.PurchaseId ORDER BY pi.Id),
        pi.CreatedAt,
        pi.CreatedByUserId
    FROM dbo.PurchaseInvoiceItems pi
    INNER JOIN dbo.Transactions t
        ON t.TransactionTypeId = @PurchaseTransactionTypeId
       AND t.ReferenceId = pi.PurchaseId
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM dbo.TransactionItems existing
        WHERE existing.TransactionId = t.Id
    );
END;

IF OBJECT_ID('dbo.UsedCarPurchases', 'U') IS NOT NULL AND @UsedCarPurchaseTransactionTypeId IS NOT NULL
BEGIN
    INSERT INTO dbo.Transactions
    (
        TransactionTypeId,
        ReferenceId,
        TransactionNumber,
        TransactionDate,
        SupplierId,
        UsedCarId,
        TotalAmount,
        PaidAmount,
        PaymentStatus,
        Notes,
        PostingStatus,
        PostedAt,
        PostedByUserId,
        BaseCurrencyCode,
        CounterCurrencyCode,
        TotalBaseAmount,
        TotalCounterAmount,
        PaidCounterAmount,
        CreatedAt,
        CreatedByUserId,
        ModifiedAt,
        ModifiedByUserId
    )
    SELECT
        @UsedCarPurchaseTransactionTypeId,
        p.Id,
        p.PurchaseNumber,
        p.PurchaseDate,
        p.SupplierId,
        p.UsedCarId,
        p.TotalBaseAmount,
        p.PaidAmount,
        p.PaymentStatus,
        p.Notes,
        p.PostingStatus,
        p.PostedAt,
        p.PostedByUserId,
        p.BaseCurrencyCode,
        p.CounterCurrencyCode,
        p.TotalBaseAmount,
        p.TotalCounterAmount,
        p.PaidCounterAmount,
        p.CreatedAt,
        p.CreatedByUserId,
        p.ModifiedAt,
        p.ModifiedByUserId
    FROM dbo.UsedCarPurchases p
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM dbo.Transactions existing
        WHERE existing.TransactionTypeId = @UsedCarPurchaseTransactionTypeId
          AND existing.ReferenceId = p.Id
    );

    INSERT INTO dbo.TransactionItems
    (
        TransactionId,
        ItemType,
        AccountId,
        DetailKey,
        Description,
        Amount,
        LineTotal,
        CurrencyCode,
        RateToBase,
        BaseAmount,
        CounterAmount,
        SortOrder,
        CreatedAt,
        CreatedByUserId,
        ModifiedAt,
        ModifiedByUserId
    )
    SELECT
        t.Id,
        N'used_car_purchase_line',
        l.AccountId,
        l.DetailKey,
        l.Description,
        l.Amount,
        l.BaseAmount,
        l.CurrencyCode,
        l.RateToBase,
        l.BaseAmount,
        l.CounterAmount,
        l.SortOrder,
        l.CreatedAt,
        l.CreatedByUserId,
        l.ModifiedAt,
        l.ModifiedByUserId
    FROM dbo.UsedCarPurchaseLines l
    INNER JOIN dbo.Transactions t
        ON t.TransactionTypeId = @UsedCarPurchaseTransactionTypeId
       AND t.ReferenceId = l.UsedCarPurchaseId
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM dbo.TransactionItems existing
        WHERE existing.TransactionId = t.Id
    );
END;

UPDATE dbo.Transactions
SET ReferenceId = Id
WHERE ReferenceId <= 0;

;WITH ParsedTransactionNumbers AS
(
    SELECT t.TransactionTypeId,
           CASE
               WHEN trailing.DigitCount > 0
                   THEN TRY_CONVERT(BIGINT, RIGHT(LTRIM(RTRIM(t.TransactionNumber)), trailing.DigitCount))
               ELSE NULL
           END AS ParsedNumber
    FROM dbo.Transactions t
    OUTER APPLY
    (
        SELECT CASE
            WHEN t.TransactionNumber IS NULL THEN 0
            ELSE PATINDEX('%[^0-9]%', REVERSE(LTRIM(RTRIM(t.TransactionNumber))) + 'X') - 1
        END AS DigitCount
    ) trailing
),
TransactionTypeMaxNumbers AS
(
    SELECT TransactionTypeId,
           MAX(ParsedNumber) AS MaxIssuedNumber
    FROM ParsedTransactionNumbers
    GROUP BY TransactionTypeId
)
UPDATE tt
SET SerialCurrentNumber = CASE
        WHEN ISNULL(m.MaxIssuedNumber, 0) > ISNULL(tt.SerialCurrentNumber, 0) THEN ISNULL(m.MaxIssuedNumber, 0)
        ELSE ISNULL(tt.SerialCurrentNumber, 0)
    END
FROM dbo.TransactionTypes tt
LEFT JOIN TransactionTypeMaxNumbers m ON m.TransactionTypeId = tt.Id;

BEGIN TRY
    ALTER TABLE dbo.TransactionTypes ALTER COLUMN SerialNumberFormat NVARCHAR(200) NOT NULL;
END TRY
BEGIN CATCH
END CATCH;

BEGIN TRY
    ALTER TABLE dbo.TransactionTypes ALTER COLUMN SerialStartNumber BIGINT NOT NULL;
END TRY
BEGIN CATCH
END CATCH;

BEGIN TRY
    ALTER TABLE dbo.TransactionTypes ALTER COLUMN SerialCurrentNumber BIGINT NOT NULL;
END TRY
BEGIN CATCH
END CATCH;

UPDATE je
SET Description = LEFT(
        CONCAT(
            N'Used car receipt - ',
            CASE
                WHEN cb.Name IS NULL OR LTRIM(RTRIM(cb.Name)) = N'' THEN
                    CASE
                        WHEN NULLIF(LTRIM(RTRIM(cm.BodyType)), N'') IS NULL THEN cm.Name
                        ELSE cm.Name + N' (' + cm.BodyType + N')'
                    END
                ELSE
                    CASE
                        WHEN NULLIF(LTRIM(RTRIM(cm.BodyType)), N'') IS NULL THEN cb.Name + N' ' + cm.Name
                        ELSE cb.Name + N' ' + cm.Name + N' (' + cm.BodyType + N')'
                    END
            END,
            N' ',
            CONVERT(NVARCHAR(4), uc.ModelYear)
        ),
        400)
FROM dbo.JournalEntries je
INNER JOIN dbo.UsedCars uc ON uc.Id = je.ReferenceId
INNER JOIN dbo.CarModels cm ON cm.Id = uc.CarModelId
INNER JOIN dbo.CarBrands cb ON cb.Id = cm.CarBrandId
WHERE je.ReferenceType = N'UsedCar';

UPDATE je
SET Description = LEFT(
        CONCAT(
            N'Used car purchase - ',
            CASE
                WHEN cb.Name IS NULL OR LTRIM(RTRIM(cb.Name)) = N'' THEN
                    CASE
                        WHEN NULLIF(LTRIM(RTRIM(cm.BodyType)), N'') IS NULL THEN cm.Name
                        ELSE cm.Name + N' (' + cm.BodyType + N')'
                    END
                ELSE
                    CASE
                        WHEN NULLIF(LTRIM(RTRIM(cm.BodyType)), N'') IS NULL THEN cb.Name + N' ' + cm.Name
                        ELSE cb.Name + N' ' + cm.Name + N' (' + cm.BodyType + N')'
                    END
            END,
            N' ',
            CONVERT(NVARCHAR(4), uc.ModelYear),
            CASE
                WHEN NULLIF(LTRIM(RTRIM(t.TransactionNumber)), N'') IS NULL THEN N''
                ELSE N' (' + LTRIM(RTRIM(t.TransactionNumber)) + N')'
            END
        ),
        400)
FROM dbo.JournalEntries je
INNER JOIN dbo.Transactions t ON t.ReferenceId = je.ReferenceId
INNER JOIN dbo.TransactionTypes tt
    ON tt.Id = t.TransactionTypeId
   AND tt.TypeKey = 'used_car_purchase'
INNER JOIN dbo.UsedCars uc ON uc.Id = t.UsedCarId
INNER JOIN dbo.CarModels cm ON cm.Id = uc.CarModelId
INNER JOIN dbo.CarBrands cb ON cb.Id = cm.CarBrandId
WHERE je.ReferenceType = N'UsedCarPurchase';

UPDATE je
SET Description = LEFT(
        CONCAT(
            N'Used car purchase payment - ',
            CASE
                WHEN cb.Name IS NULL OR LTRIM(RTRIM(cb.Name)) = N'' THEN
                    CASE
                        WHEN NULLIF(LTRIM(RTRIM(cm.BodyType)), N'') IS NULL THEN cm.Name
                        ELSE cm.Name + N' (' + cm.BodyType + N')'
                    END
                ELSE
                    CASE
                        WHEN NULLIF(LTRIM(RTRIM(cm.BodyType)), N'') IS NULL THEN cb.Name + N' ' + cm.Name
                        ELSE cb.Name + N' ' + cm.Name + N' (' + cm.BodyType + N')'
                    END
            END,
            N' ',
            CONVERT(NVARCHAR(4), uc.ModelYear),
            CASE
                WHEN NULLIF(LTRIM(RTRIM(t.TransactionNumber)), N'') IS NULL THEN N''
                ELSE N' (' + LTRIM(RTRIM(t.TransactionNumber)) + N')'
            END
        ),
        400)
FROM dbo.JournalEntries je
INNER JOIN dbo.Transactions t ON t.ReferenceId = je.ReferenceId
INNER JOIN dbo.TransactionTypes tt
    ON tt.Id = t.TransactionTypeId
   AND tt.TypeKey = 'used_car_purchase'
INNER JOIN dbo.UsedCars uc ON uc.Id = t.UsedCarId
INNER JOIN dbo.CarModels cm ON cm.Id = uc.CarModelId
INNER JOIN dbo.CarBrands cb ON cb.Id = cm.CarBrandId
WHERE je.ReferenceType = N'UsedCarPurchasePayment';
");
    }
}
