namespace SpareParts.Domain.Communications
{
    public sealed class CommunicationConversationDto
    {
        public string RecipientName { get; set; } = string.Empty;
        public string RecipientPhone { get; set; } = string.Empty;
        public string RecipientKind { get; set; } = string.Empty;
        public int? RecipientId { get; set; }
        public string LastMessagePreview { get; set; } = string.Empty;
        public string LastDirection { get; set; } = string.Empty;
        public string LastStatus { get; set; } = string.Empty;
        public DateTime LastMessageAt { get; set; }
        public int MessageCount { get; set; }
        public int InboundCount { get; set; }
        public int OutboundCount { get; set; }
        public int UnreadCount { get; set; }
    }
}
