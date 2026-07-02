namespace SpareParts.Infrastructure.Services.Pricing;

/// <summary>Raw <c>dbo.WebhookEvents</c> row used by <see cref="PaymentService"/>.</summary>
internal sealed class WebhookEventRow
{
    public int Id { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string EventId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public int Status { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string? Error { get; set; }
    public DateTime CreatedAt { get; set; }
}
