using System.Collections.Generic;

namespace SpareParts.Desktop.Wpf.Interfaces
{
    /// <summary>Unpaid-transactions panel of GET /api/dashboard/summary (Report 04 §03).</summary>
    public sealed class IgnitionUnpaidTransactionsPanelDto
    {
        public int Count { get; set; }
        public decimal TotalRemaining { get; set; }
        public List<IgnitionUnpaidTransactionItemDto> TopItems { get; set; } = new();
    }
}
