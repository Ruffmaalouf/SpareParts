namespace SpareParts.Domain.Communications
{
    public static class WhatsAppCampaignSegments
    {
        public const string AllCustomers = "AllCustomers";
        public const string ActiveCustomers = "ActiveCustomers";
        public const string InactiveCustomers = "InactiveCustomers";
        public const string UnpaidCustomers = "UnpaidCustomers";
        public const string RecentBuyers = "RecentBuyers";
        public const string WaitingForParts = "WaitingForParts";
    }

    public sealed class WhatsAppCampaignAssetDto
    {
        public string AssetType { get; set; } = string.Empty;
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Subtitle { get; set; } = string.Empty;
        public decimal? Price { get; set; }
        public string? Currency { get; set; }
        public int ImageCount { get; set; }
        public bool IsSelected { get; set; }
    }

    public sealed class WhatsAppCampaignRecipientDto
    {
        public int? CustomerId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public DateTime? LastSaleAt { get; set; }
        public int SaleCount { get; set; }
        public decimal TotalSales { get; set; }
        public decimal OutstandingBalance { get; set; }
        public DateTime? LastInboundAt { get; set; }
        public DateTime? LastOutboundAt { get; set; }
    }

    public class WhatsAppCampaignPreviewRequest
    {
        public string Segment { get; set; } = WhatsAppCampaignSegments.AllCustomers;
        public List<int> CustomerIds { get; set; } = new();
        public List<int> PartIds { get; set; } = new();
        public List<int> UsedCarIds { get; set; } = new();
        public string Language { get; set; } = "ArabicEnglish";
        public bool IncludeImages { get; set; } = true;
        public string? Note { get; set; }
        public int MaxRecipients { get; set; } = 50;
    }

    public sealed class WhatsAppCampaignPreviewResponse
    {
        public string Segment { get; set; } = WhatsAppCampaignSegments.AllCustomers;
        public string Language { get; set; } = "ArabicEnglish";
        public string MessageBody { get; set; } = string.Empty;
        public int RecipientCount { get; set; }
        public int AttachmentCount { get; set; }
        public IReadOnlyList<WhatsAppCampaignRecipientDto> Recipients { get; set; } = [];
        public IReadOnlyList<WhatsAppCampaignAssetDto> Assets { get; set; } = [];
    }

    public sealed class SendWhatsAppCampaignRequest : WhatsAppCampaignPreviewRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? MessageBodyOverride { get; set; }
    }

    public sealed class WhatsAppCampaignSendResponse
    {
        public int CampaignId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int RecipientCount { get; set; }
        public int SentCount { get; set; }
        public int FailedCount { get; set; }
        public int PreparedCount { get; set; }
        public int AttachmentCount { get; set; }
        public string MessageBody { get; set; } = string.Empty;
    }

    public sealed class WhatsAppCampaignSummaryDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Segment { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
        public string MessageBody { get; set; } = string.Empty;
        public int RecipientCount { get; set; }
        public int SentCount { get; set; }
        public int FailedCount { get; set; }
        public int PreparedCount { get; set; }
        public int ReplyCount { get; set; }
        public int SalesCount { get; set; }
        public decimal SalesAmount { get; set; }
        public int AttachmentCount { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
