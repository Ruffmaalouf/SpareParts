namespace SpareParts.Domain.Concierge;

public class ChatMessage
{
    public ChatMessageRole Role { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}
