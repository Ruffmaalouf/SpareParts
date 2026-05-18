namespace SpareParts.Domain.Communications
{
    public sealed class CommunicationConversationMessageDto
    {
        public int Id { get; set; }
        public string Direction { get; set; } = string.Empty;
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
        public DateTime? SentAt { get; set; }
    }
}
