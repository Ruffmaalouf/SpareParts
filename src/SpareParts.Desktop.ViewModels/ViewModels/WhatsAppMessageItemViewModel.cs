using SpareParts.Domain.Communications;
using System;

namespace SpareParts.Desktop.Wpf.ViewModels
{
    public sealed class WhatsAppMessageItemViewModel
    {
        public WhatsAppMessageItemViewModel(CommunicationConversationMessageDto dto)
        {
            Id = dto.Id;
            Direction = dto.Direction;
            Channel = dto.Channel;
            Body = dto.Body;
            Status = dto.Status;
            TemplateKey = dto.TemplateKey;
            AttachmentCount = dto.AttachmentCount;
            ProviderStatus = dto.ProviderStatus;
            ErrorMessage = dto.ErrorMessage;
            CreatedAt = dto.CreatedAt;
            SentAt = dto.SentAt;
        }

        public int Id { get; }
        public string Direction { get; }
        public string Channel { get; }
        public string Body { get; }
        public string Status { get; }
        public string TemplateKey { get; }
        public int AttachmentCount { get; }
        public string? ProviderStatus { get; }
        public string? ErrorMessage { get; }
        public DateTime CreatedAt { get; }
        public DateTime? SentAt { get; }

        public bool IsOutbound => string.Equals(Direction, "Outbound", StringComparison.OrdinalIgnoreCase);
        public bool HasAttachments => AttachmentCount > 0;
        public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
        public string TimeText => (SentAt ?? CreatedAt).ToLocalTime().ToString("HH:mm");
        public string StatusText => HasError
            ? ErrorMessage!
            : HasAttachments
                ? $"{Status} · {AttachmentCount:N0} attachment(s)"
                : Status;
    }
}
