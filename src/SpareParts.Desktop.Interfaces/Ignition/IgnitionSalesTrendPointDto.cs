using System;

namespace SpareParts.Desktop.Wpf.Interfaces
{
    /// <summary>One point of the dashboard sales trend series (Report 04 §03).</summary>
    public sealed class IgnitionSalesTrendPointDto
    {
        public DateTime Date { get; set; }
        public decimal Amount { get; set; }
    }
}
