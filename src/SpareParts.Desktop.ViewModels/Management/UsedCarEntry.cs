namespace SpareParts.Desktop.Wpf.Management
{
    public class UsedCarEntry
    {
        public string Car { get; set; } = string.Empty;
        public int ModelYear { get; set; }
        public string PriceCurrency { get; set; } = "USD";
        public decimal PriceBase { get; set; }
        public decimal PriceCounter { get; set; }
        public string Location { get; set; } = string.Empty;
        public decimal Transportation { get; set; }
        public decimal TotalBeforeShipping => PriceCounter + Transportation;
        public string PartOut { get; set; } = string.Empty;
        public decimal Shipping { get; set; }
        public decimal Customs { get; set; }
    }
}
