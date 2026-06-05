using Dapper;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Api.Infrastructure;

public static class QuotesMigration
{
    public static void EnsureApplied(ISqlConnectionFactory factory)
    {
        using var conn = factory.CreateConnection();
        conn.Execute(
            """
IF OBJECT_ID('dbo.Quotes', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Quotes
    (
        Id                INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Quotes PRIMARY KEY,
        QuoteNumber       NVARCHAR(50)      NOT NULL CONSTRAINT DF_Quotes_QuoteNumber DEFAULT N'',
        QuoteDate         DATETIME2(0)      NOT NULL,
        ExpiryDate        DATETIME2(0)      NULL,
        CustomerId        INT               NULL,
        CustomerName      NVARCHAR(200)     NOT NULL CONSTRAINT DF_Quotes_CustomerName DEFAULT N'',
        CustomerPhone     NVARCHAR(50)      NULL,
        WarehouseId       INT               NULL,
        Status            NVARCHAR(30)      NOT NULL CONSTRAINT DF_Quotes_Status DEFAULT N'Draft',
        Notes             NVARCHAR(1000)    NULL,
        CreatedAt         DATETIME2(0)      NOT NULL CONSTRAINT DF_Quotes_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedByUserId   INT               NULL,
        CONSTRAINT FK_Quotes_Customers   FOREIGN KEY (CustomerId)  REFERENCES dbo.Customers (Id),
        CONSTRAINT FK_Quotes_Warehouses  FOREIGN KEY (WarehouseId) REFERENCES dbo.Warehouses (Id)
    );
END;

IF OBJECT_ID('dbo.QuoteItems', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.QuoteItems
    (
        Id              INT IDENTITY(1,1)  NOT NULL CONSTRAINT PK_QuoteItems PRIMARY KEY,
        QuoteId         INT                NOT NULL,
        PartId          INT                NULL,
        Description     NVARCHAR(500)      NOT NULL CONSTRAINT DF_QuoteItems_Description DEFAULT N'',
        Quantity        INT                NOT NULL CONSTRAINT DF_QuoteItems_Quantity DEFAULT 1,
        UnitPrice       DECIMAL(19,4)      NOT NULL CONSTRAINT DF_QuoteItems_UnitPrice DEFAULT 0,
        DiscountAmount  DECIMAL(19,4)      NOT NULL CONSTRAINT DF_QuoteItems_Discount DEFAULT 0,
        SortOrder       INT                NOT NULL CONSTRAINT DF_QuoteItems_SortOrder DEFAULT 0,
        CONSTRAINT FK_QuoteItems_Quotes FOREIGN KEY (QuoteId) REFERENCES dbo.Quotes (Id),
        CONSTRAINT FK_QuoteItems_Parts  FOREIGN KEY (PartId)  REFERENCES dbo.Parts (Id)
    );
END;
""");
    }
}
