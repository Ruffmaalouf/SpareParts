namespace SpareParts.Infrastructure.Services
{
    internal sealed class OpenAiOutputItem
    {
        public string? Type { get; set; }
        public List<OpenAiContentItem>? Content { get; set; }
    }
}
