namespace SpareParts.Desktop.Wpf.ViewModels
{
    public sealed class PurchaseDraftItemViewModel
    {
        public string PartCode { get; set; } = string.Empty;
        public string PartName { get; set; } = string.Empty;
        public string SupplierName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitCost { get; set; }
        public decimal LineTotal => Quantity * UnitCost;
    }
}
