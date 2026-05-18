using System.Collections.Generic;

namespace SpareParts.Domain.BusinessAssistant
{
    public sealed class BusinessAssistantResponseDto
    {
        public string Intent { get; set; } = "unknown";
        public bool IsSupported { get; set; }
        public string Answer { get; set; } = string.Empty;
        public List<BusinessAssistantInsightDto> Insights { get; set; } = new();
        public List<BusinessAssistantActionDto> Actions { get; set; } = new();
        public List<string> Suggestions { get; set; } = new();
    }
}
