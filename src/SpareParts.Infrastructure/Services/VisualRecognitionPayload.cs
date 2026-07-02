namespace SpareParts.Infrastructure.Services;

/// <summary>Structured JSON payload returned by the AI vision model for <see cref="VisualPartSearchService"/>.</summary>
internal sealed class VisualRecognitionPayload
{
    public string? PartFamily { get; set; }
    public string? SearchText { get; set; }
    public List<string>? Tags { get; set; }
    public decimal Confidence { get; set; }
}
