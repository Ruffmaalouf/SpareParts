namespace SpareParts.Domain.BusinessAssistant
{
    public sealed class BusinessAssistantInsightDto
    {
        public string Label { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string Detail { get; set; } = string.Empty;
        public string Severity { get; set; } = "Neutral";
    }
}
