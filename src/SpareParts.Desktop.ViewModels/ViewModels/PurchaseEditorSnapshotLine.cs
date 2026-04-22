namespace SpareParts.Desktop.Wpf.ViewModels
{
    internal sealed class PurchaseEditorSnapshotLine
    {
        public int PartId { get; set; }
        public string Description { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitCost { get; set; }
    }
}
