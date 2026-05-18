using Dapper;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Api.Infrastructure;

public static class CommunicationsMigration
{
    public static void EnsureApplied(ISqlConnectionFactory factory)
    {
        using var conn = factory.CreateConnection();
        conn.Execute(
            @"
IF OBJECT_ID('dbo.OutboundMessages', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.OutboundMessages
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_OutboundMessages PRIMARY KEY,
        Direction NVARCHAR(20) NOT NULL CONSTRAINT DF_OutboundMessages_Direction DEFAULT (N'Outbound'),
        Channel NVARCHAR(20) NOT NULL,
        RecipientKind NVARCHAR(20) NOT NULL,
        RecipientId INT NULL,
        RecipientName NVARCHAR(200) NOT NULL,
        RecipientPhone NVARCHAR(50) NOT NULL,
        TemplateKey NVARCHAR(60) NOT NULL,
        ReferenceType NVARCHAR(60) NOT NULL,
        ReferenceId INT NULL,
        Body NVARCHAR(MAX) NOT NULL,
        AttachmentCount INT NOT NULL CONSTRAINT DF_OutboundMessages_AttachmentCount DEFAULT (0),
        Status NVARCHAR(30) NOT NULL,
        Provider NVARCHAR(80) NULL,
        ProviderMessageId NVARCHAR(200) NULL,
        ProviderStatus NVARCHAR(200) NULL,
        ErrorMessage NVARCHAR(1000) NULL,
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_OutboundMessages_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedByUserId INT NULL,
        SentAt DATETIME2(0) NULL,
        CONSTRAINT FK_OutboundMessages_CreatedByUsers FOREIGN KEY (CreatedByUserId) REFERENCES dbo.Users (Id)
    );
END;

IF COL_LENGTH('dbo.OutboundMessages', 'Direction') IS NULL
BEGIN
    ALTER TABLE dbo.OutboundMessages
    ADD Direction NVARCHAR(20) NOT NULL CONSTRAINT DF_OutboundMessages_Direction DEFAULT (N'Outbound') WITH VALUES;
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_OutboundMessages_CreatedAt'
      AND object_id = OBJECT_ID('dbo.OutboundMessages'))
BEGIN
    CREATE INDEX IX_OutboundMessages_CreatedAt
        ON dbo.OutboundMessages (CreatedAt DESC, Id DESC);
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_OutboundMessages_RecipientPhone'
      AND object_id = OBJECT_ID('dbo.OutboundMessages'))
BEGIN
    CREATE INDEX IX_OutboundMessages_RecipientPhone
        ON dbo.OutboundMessages (RecipientPhone, CreatedAt DESC, Id DESC);
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_OutboundMessages_Reference'
      AND object_id = OBJECT_ID('dbo.OutboundMessages'))
BEGIN
    CREATE INDEX IX_OutboundMessages_Reference
        ON dbo.OutboundMessages (ReferenceType, ReferenceId, CreatedAt DESC);
END;");
    }
}
