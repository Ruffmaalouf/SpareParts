using System.Collections.Generic;

namespace SpareParts.Desktop.Wpf.Interfaces
{
    /// <summary>Dead-stock panel of GET /api/dashboard/summary (Report 04 §03).</summary>
    public sealed class IgnitionDeadStockPanelDto
    {
        public decimal TotalValue { get; set; }
        public int TotalUnits { get; set; }
        public List<IgnitionDeadStockSegmentDto> TopSegments { get; set; } = new();
    }
}
