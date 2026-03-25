namespace SpareParts.Desktop.Wpf.ViewModels
{
    public sealed class StockSnapshotViewModel
    {
        public string PartCode { get; set; } = string.Empty;
        public string PartName { get; set; } = string.Empty;
        public string Warehouse { get; set; } = string.Empty;
        public int OnHand { get; set; }
        public int MinStock { get; set; }
        public string Status => OnHand <= MinStock ? "Low Stock" : "Healthy";
    }
}
