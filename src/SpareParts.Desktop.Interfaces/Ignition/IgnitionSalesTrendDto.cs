using System;
using System.Collections.Generic;

namespace SpareParts.Desktop.Wpf.Interfaces
{
    /// <summary>Sales trend block of GET /api/dashboard/summary (Report 04 §03).</summary>
    public sealed class IgnitionSalesTrendDto
    {
        /// <summary>"daily" for the default 7-day range.</summary>
        public string Granularity { get; set; } = "daily";

        public int RangeDays { get; set; }
        public List<IgnitionSalesTrendPointDto> Series { get; set; } = new();
        public DateTime? PeakDate { get; set; }
        public decimal? PeakAmount { get; set; }
    }
}
