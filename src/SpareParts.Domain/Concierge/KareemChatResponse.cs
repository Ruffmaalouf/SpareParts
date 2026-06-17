namespace SpareParts.Domain.Concierge;

public class KareemChatResponse
{
    public string Reply { get; set; } = string.Empty;
    public string Intent { get; set; } = string.Empty;
    public string Language { get; set; } = "en";
    public List<string> SuggestedActions { get; set; } = new();
}
