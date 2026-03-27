namespace SpareParts.Desktop.Wpf.ViewModels
{
    internal sealed class InvoiceSnapshot
    {
        public int? CustomerId { get; init; }
        public int? WarehouseId { get; init; }
        public decimal PaidAmount { get; init; }
        public DateTime InvoiceDate { get; init; }
        public List<InvoiceSnapshotLine> Items { get; init; } = new();
    }
}
