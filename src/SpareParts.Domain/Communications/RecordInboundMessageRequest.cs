namespace SpareParts.Domain.Communications
{
    public sealed class RecordInboundMessageRequest
    {
        public CommunicationChannel Channel { get; set; } = CommunicationChannel.WhatsApp;
        public string? SenderName { get; set; }
        public string SenderPhone { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public string? Provider { get; set; }
        public string? ProviderMessageId { get; set; }
        public string? ProviderStatus { get; set; }
        public DateTime? ReceivedAt { get; set; }
    }
}
