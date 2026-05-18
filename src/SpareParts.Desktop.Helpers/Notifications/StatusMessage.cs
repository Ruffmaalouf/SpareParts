using System;

namespace SpareParts.Desktop.Wpf
{
    public sealed class StatusMessage
    {
        public required string Text { get; init; }
        public required bool IsSuccess { get; init; }
        public DateTime CreatedAtUtc { get; init; } = DateTime.UtcNow;
    }
}
