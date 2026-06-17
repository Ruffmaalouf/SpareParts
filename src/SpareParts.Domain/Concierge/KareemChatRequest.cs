namespace SpareParts.Domain.Concierge;

public class KareemChatRequest
{
    public string Message { get; set; } = string.Empty;
    public string Language { get; set; } = "en";
    public List<ChatMessage> History { get; set; } = new();
}
