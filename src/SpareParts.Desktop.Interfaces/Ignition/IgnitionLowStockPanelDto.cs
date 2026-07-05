using System.Collections.Generic;

namespace SpareParts.Desktop.Wpf.Interfaces
{
    /// <summary>Low-stock panel of GET /api/dashboard/summary (Report 04 §03).</summary>
    public sealed class IgnitionLowStockPanelDto
    {
        public int TotalCount { get; set; }
        public List<IgnitionLowStockItemDto> TopItems { get; set; } = new();
    }
}
