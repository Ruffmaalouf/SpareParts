using SpareParts.Domain.Reports;

namespace SpareParts.Desktop.Wpf
{
    internal sealed class ReportBuilderColumnDragPayload
    {
        public string Role { get; init; } = string.Empty;
        public ReportColumnDto Column { get; init; } = new();
    }
}
