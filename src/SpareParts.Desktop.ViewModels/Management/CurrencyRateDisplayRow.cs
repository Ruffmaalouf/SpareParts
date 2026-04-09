using System;

namespace SpareParts.Desktop.Wpf.Management
{
    public sealed class CurrencyRateDisplayRow
    {
        public string Code { get; init; } = string.Empty;
        public decimal BaseRate { get; init; }
        public decimal CounterRate { get; init; }
        public DateTime? SnapshotUtc { get; init; }
    }
}
