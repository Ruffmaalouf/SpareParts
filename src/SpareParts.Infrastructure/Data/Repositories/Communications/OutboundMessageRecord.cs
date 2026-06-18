namespace SpareParts.Infrastructure.Data
{
    public sealed class OutboundMessageRecord
    {
        public string Direction { get; set; } = "Outbound";
        public string Channel { get; set; } = string.Empty;
        public string RecipientKind { get; set; } = string.Empty;
        public int? RecipientId { get; set; }
        public string RecipientName { get; set; } = string.Empty;
        public string RecipientPhone { get; set; } = string.Empty;
        public string TemplateKey { get; set; } = string.Empty;
        public string ReferenceType { get; set; } = string.Empty;
        public int? ReferenceId { get; set; }
        public string Body { get; set; } = string.Empty;
        public int AttachmentCount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Provider { get; set; }
        public string? ProviderMessageId { get; set; }
        public string? ProviderStatus { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? CreatedByUserId { get; set; }
        public DateTime? SentAt { get; set; }
        public int? TenantId { get; set; }
    }
}
