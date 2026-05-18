namespace SpareParts.Domain.Communications
{
    public sealed class CommunicationAttachmentDto
    {
        public int? SourceId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string MimeType { get; set; } = "application/octet-stream";
        public string Base64Content { get; set; } = string.Empty;
    }
}
