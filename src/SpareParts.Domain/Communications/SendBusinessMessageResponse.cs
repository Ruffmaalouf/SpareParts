namespace SpareParts.Domain.Communications
{
    public sealed class SendBusinessMessageResponse
    {
        public int MessageId { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Channel { get; set; } = string.Empty;
        public string TemplateKey { get; set; } = string.Empty;
        public string RecipientName { get; set; } = string.Empty;
        public string RecipientPhone { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public int AttachmentCount { get; set; }
        public string? Provider { get; set; }
        public string? ProviderMessageId { get; set; }
        public string? ProviderStatus { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
