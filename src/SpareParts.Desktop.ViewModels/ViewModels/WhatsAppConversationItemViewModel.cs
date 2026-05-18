using SpareParts.Domain.Communications;
using System;

namespace SpareParts.Desktop.Wpf.ViewModels
{
    public sealed class WhatsAppConversationItemViewModel
    {
        public WhatsAppConversationItemViewModel(CommunicationConversationDto dto)
        {
            RecipientName = string.IsNullOrWhiteSpace(dto.RecipientName) ? dto.RecipientPhone : dto.RecipientName;
            RecipientPhone = dto.RecipientPhone;
            RecipientKind = dto.RecipientKind;
            RecipientId = dto.RecipientId;
            LastMessagePreview = dto.LastMessagePreview;
            LastDirection = dto.LastDirection;
            LastStatus = dto.LastStatus;
            LastMessageAt = dto.LastMessageAt;
            MessageCount = dto.MessageCount;
            InboundCount = dto.InboundCount;
            OutboundCount = dto.OutboundCount;
            UnreadCount = dto.UnreadCount;
        }

        public string RecipientName { get; }
        public string RecipientPhone { get; }
        public string RecipientKind { get; }
        public int? RecipientId { get; }
        public string LastMessagePreview { get; }
        public string LastDirection { get; }
        public string LastStatus { get; }
        public DateTime LastMessageAt { get; }
        public int MessageCount { get; }
        public int InboundCount { get; }
        public int OutboundCount { get; }
        public int UnreadCount { get; }

        public string Initials => BuildInitials(RecipientName);
        public string LastMessageTimeText => LastMessageAt.ToLocalTime().ToString("MMM d, HH:mm");
        public string DirectionLabel => string.Equals(LastDirection, "Inbound", StringComparison.OrdinalIgnoreCase) ? "From contact" : "Sent";
        public bool HasUnread => UnreadCount > 0;

        private static string BuildInitials(string value)
        {
            var words = (value ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (words.Length == 0)
            {
                return "WA";
            }

            if (words.Length == 1)
            {
                return words[0].Length == 1
                    ? words[0].ToUpperInvariant()
                    : words[0][..Math.Min(2, words[0].Length)].ToUpperInvariant();
            }

            return $"{words[0][0]}{words[1][0]}".ToUpperInvariant();
        }
    }
}
