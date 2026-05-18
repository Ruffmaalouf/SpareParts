using Dapper;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Api.Infrastructure;

public static class WhatsAppCampaignsMigration
{
    public static void EnsureApplied(ISqlConnectionFactory factory)
    {
        using var conn = factory.CreateConnection();
        conn.Execute(
            """
IF OBJECT_ID('dbo.WhatsAppCampaigns', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.WhatsAppCampaigns
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_WhatsAppCampaigns PRIMARY KEY,
        Name NVARCHAR(160) NOT NULL,
        Segment NVARCHAR(60) NOT NULL,
        Language NVARCHAR(30) NOT NULL,
        MessageBody NVARCHAR(MAX) NOT NULL,
        SelectedPartIds NVARCHAR(MAX) NULL,
        SelectedUsedCarIds NVARCHAR(MAX) NULL,
        RecipientCount INT NOT NULL CONSTRAINT DF_WhatsAppCampaigns_RecipientCount DEFAULT (0),
        SentCount INT NOT NULL CONSTRAINT DF_WhatsAppCampaigns_SentCount DEFAULT (0),
        FailedCount INT NOT NULL CONSTRAINT DF_WhatsAppCampaigns_FailedCount DEFAULT (0),
        PreparedCount INT NOT NULL CONSTRAINT DF_WhatsAppCampaigns_PreparedCount DEFAULT (0),
        AttachmentCount INT NOT NULL CONSTRAINT DF_WhatsAppCampaigns_AttachmentCount DEFAULT (0),
        Status NVARCHAR(30) NOT NULL CONSTRAINT DF_WhatsAppCampaigns_Status DEFAULT (N'Draft'),
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_WhatsAppCampaigns_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedByUserId INT NULL,
        CONSTRAINT FK_WhatsAppCampaigns_CreatedByUsers FOREIGN KEY (CreatedByUserId) REFERENCES dbo.Users (Id)
    );
END;

IF OBJECT_ID('dbo.WhatsAppCampaignRecipients', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.WhatsAppCampaignRecipients
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_WhatsAppCampaignRecipients PRIMARY KEY,
        CampaignId INT NOT NULL,
        CustomerId INT NULL,
        RecipientName NVARCHAR(200) NOT NULL,
        RecipientPhone NVARCHAR(50) NOT NULL,
        MessageId INT NULL,
        Status NVARCHAR(30) NOT NULL CONSTRAINT DF_WhatsAppCampaignRecipients_Status DEFAULT (N'Prepared'),
        ErrorMessage NVARCHAR(1000) NULL,
        SentAt DATETIME2(0) NULL,
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_WhatsAppCampaignRecipients_CreatedAt DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_WhatsAppCampaignRecipients_Campaigns FOREIGN KEY (CampaignId) REFERENCES dbo.WhatsAppCampaigns (Id) ON DELETE CASCADE,
        CONSTRAINT FK_WhatsAppCampaignRecipients_Customers FOREIGN KEY (CustomerId) REFERENCES dbo.Customers (Id),
        CONSTRAINT FK_WhatsAppCampaignRecipients_Messages FOREIGN KEY (MessageId) REFERENCES dbo.OutboundMessages (Id)
    );
END;

IF OBJECT_ID('dbo.WhatsAppCampaigns', 'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.WhatsAppCampaigns') AND name = 'IX_WhatsAppCampaigns_CreatedAt')
BEGIN
    CREATE INDEX IX_WhatsAppCampaigns_CreatedAt ON dbo.WhatsAppCampaigns (CreatedAt DESC, Id DESC);
END;

IF OBJECT_ID('dbo.WhatsAppCampaignRecipients', 'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.WhatsAppCampaignRecipients') AND name = 'IX_WhatsAppCampaignRecipients_CampaignId')
BEGIN
    CREATE INDEX IX_WhatsAppCampaignRecipients_CampaignId ON dbo.WhatsAppCampaignRecipients (CampaignId, Id);
END;

IF OBJECT_ID('dbo.WhatsAppCampaignRecipients', 'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.WhatsAppCampaignRecipients') AND name = 'IX_WhatsAppCampaignRecipients_Phone')
BEGIN
    CREATE INDEX IX_WhatsAppCampaignRecipients_Phone ON dbo.WhatsAppCampaignRecipients (RecipientPhone, CampaignId);
END;
""");
    }
}
