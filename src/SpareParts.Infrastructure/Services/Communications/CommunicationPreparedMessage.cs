using SpareParts.Domain.Communications;

namespace SpareParts.Infrastructure.Services
{
    public sealed class CommunicationPreparedMessage
    {
        public string Channel { get; set; } = string.Empty;
        public string RecipientKind { get; set; } = string.Empty;
        public int? RecipientId { get; set; }
        public string RecipientName { get; set; } = string.Empty;
        public string RecipientPhone { get; set; } = string.Empty;
        public string TemplateKey { get; set; } = string.Empty;
        public string ReferenceType { get; set; } = string.Empty;
        public int? ReferenceId { get; set; }
        public string Body { get; set; } = string.Empty;
        public List<CommunicationAttachmentDto> Attachments { get; set; } = new();
    }
}
