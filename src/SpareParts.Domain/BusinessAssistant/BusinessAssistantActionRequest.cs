using System.Collections.Generic;

namespace SpareParts.Domain.BusinessAssistant
{
    public sealed class BusinessAssistantActionRequest
    {
        public string ActionId { get; set; } = string.Empty;
        public Dictionary<string, string> Payload { get; set; } = new();
    }
}
