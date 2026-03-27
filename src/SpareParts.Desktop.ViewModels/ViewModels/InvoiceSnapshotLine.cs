namespace SpareParts.Desktop.Wpf.ViewModels
{
    internal sealed class InvoiceSnapshotLine
    {
        public int PartId { get; init; }
        public string Description { get; init; } = string.Empty;
        public int Quantity { get; init; }
        public decimal UnitPrice { get; init; }
    }
}
