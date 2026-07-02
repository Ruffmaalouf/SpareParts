namespace SpareParts.Infrastructure.Services;

/// <summary>Result of attempting AI-vision recognition on an uploaded part photo, used by <see cref="VisualPartSearchService"/>.</summary>
internal sealed record VisualRecognitionResult(
    string Source,
    string SearchText,
    string PartFamily,
    IReadOnlyList<string> Tags,
    string Message)
{
    public static VisualRecognitionResult Metadata(string message)
        => new("metadata", string.Empty, string.Empty, Array.Empty<string>(), message);
}
