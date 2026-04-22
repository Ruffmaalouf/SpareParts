namespace SpareParts.Infrastructure.Services
{
    internal sealed class OpenAiContentItem
    {
        public string? Type { get; set; }
        public string? Text { get; set; }
        public string? Refusal { get; set; }
    }
}
