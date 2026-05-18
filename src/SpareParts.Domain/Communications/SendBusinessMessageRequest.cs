namespace SpareParts.Domain.Communications
{
    public sealed class SendBusinessMessageRequest
    {
        public CommunicationChannel Channel { get; set; } = CommunicationChannel.WhatsApp;
        public CommunicationTemplateKey TemplateKey { get; set; } = CommunicationTemplateKey.SalesInvoice;
        public CommunicationRecipientKind RecipientKind { get; set; } = CommunicationRecipientKind.Customer;
        public int? RecipientId { get; set; }
        public string? RecipientPhoneOverride { get; set; }
        public string? RecipientNameOverride { get; set; }
        public int? SalesInvoiceId { get; set; }
        public int? PurchaseInvoiceId { get; set; }
        public int? AccountId { get; set; }
        public DateTime? StatementDateFrom { get; set; }
        public DateTime? StatementDateTo { get; set; }
        public int? PartId { get; set; }
        public int? WarehouseId { get; set; }
        public int? UsedCarId { get; set; }
        public List<int> UsedCarImageIds { get; set; } = new();
        public decimal? QuotedPrice { get; set; }
        public string? MessageBody { get; set; }
        public string? Note { get; set; }
        public int? CampaignId { get; set; }
        public List<CommunicationAttachmentDto> Attachments { get; set; } = new();
        public bool PreviewOnly { get; set; }
    }
}
