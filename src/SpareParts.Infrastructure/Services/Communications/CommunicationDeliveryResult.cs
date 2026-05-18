namespace SpareParts.Infrastructure.Services
{
    public sealed class CommunicationDeliveryResult
    {
        public string Status { get; set; } = "Failed";
        public string? Provider { get; set; }
        public string? ProviderMessageId { get; set; }
        public string? ProviderStatus { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime? SentAt { get; set; }
    }
}
