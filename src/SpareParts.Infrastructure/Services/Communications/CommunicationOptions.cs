namespace SpareParts.Infrastructure.Services
{
    public sealed class CommunicationOptions
    {
        public string Provider { get; set; } = "Disabled";
        public string? WebhookUrl { get; set; }
        public string? WebhookSecret { get; set; }
        public int TimeoutSeconds { get; set; } = 15;

        public bool HasWebhook =>
            !string.IsNullOrWhiteSpace(WebhookUrl)
            && Uri.TryCreate(WebhookUrl, UriKind.Absolute, out _);
    }
}
