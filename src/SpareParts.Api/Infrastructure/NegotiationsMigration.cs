using Dapper;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Api.Infrastructure;

public static class NegotiationsMigration
{
    public static void EnsureApplied(ISqlConnectionFactory factory)
    {
        using var conn = factory.CreateConnection();
        conn.Execute("""
IF OBJECT_ID('dbo.Negotiations', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Negotiations
    (
        Id                 INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Negotiations PRIMARY KEY,
        TenantId           INT NOT NULL CONSTRAINT DF_Negotiations_TenantId DEFAULT 0,
        PartId             INT NOT NULL,
        BuyerName          NVARCHAR(200) NOT NULL,
        BuyerPhone         NVARCHAR(50) NULL,
        AskingPrice        DECIMAL(18,4) NOT NULL,
        BuyerMaxPrice      DECIMAL(18,4) NULL,
        SellerMinPrice     DECIMAL(18,4) NULL,
        CurrentOffer       DECIMAL(18,4) NULL,
        Status             TINYINT NOT NULL CONSTRAINT DF_Negotiations_Status DEFAULT 1,
        Currency           NVARCHAR(10) NOT NULL CONSTRAINT DF_Negotiations_Currency DEFAULT 'USD',
        CreatedAt          DATETIME2 NOT NULL CONSTRAINT DF_Negotiations_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedByUserId    INT NULL
    );
    CREATE INDEX IX_Negotiations_TenantId ON dbo.Negotiations (TenantId);
END;
IF OBJECT_ID('dbo.NegotiationOffers', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.NegotiationOffers
    (
        Id                 INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_NegotiationOffers PRIMARY KEY,
        SessionId          INT NOT NULL,
        OfferedByRole      NVARCHAR(20) NOT NULL,
        Amount             DECIMAL(18,4) NOT NULL,
        Note               NVARCHAR(500) NULL,
        CreatedAt          DATETIME2 NOT NULL CONSTRAINT DF_NegotiationOffers_CreatedAt DEFAULT SYSUTCDATETIME()
    );
    CREATE INDEX IX_NegotiationOffers_SessionId ON dbo.NegotiationOffers (SessionId);
END;
""");
    }
}
