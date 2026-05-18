using System.Collections.Generic;

namespace SpareParts.Domain.BusinessAssistant
{
    public sealed class BusinessAssistantActionDto
    {
        public string Id { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Kind { get; set; } = "Query";
        public string Target { get; set; } = string.Empty;
        public string Tone { get; set; } = "Neutral";
        public Dictionary<string, string> Payload { get; set; } = new();
    }
}
