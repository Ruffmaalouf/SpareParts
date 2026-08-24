namespace SpareParts.Desktop.Wpf.Interfaces
{
    /// <summary>Aging/balance block of GET /api/clients/{id}/workspace (Report 04 §05).</summary>
    public sealed class IgnitionClientBalanceDto
    {
        public decimal TotalBalance { get; set; }
        public decimal Current { get; set; }
        public decimal Days1To30 { get; set; }
        public decimal Days31To60 { get; set; }
        public decimal Days61To90 { get; set; }
        public decimal Over90Days { get; set; }
    }
}
