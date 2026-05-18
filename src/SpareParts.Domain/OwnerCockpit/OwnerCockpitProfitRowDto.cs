namespace SpareParts.Domain.OwnerCockpit
{
    public sealed class OwnerCockpitProfitRowDto
    {
        public string Name { get; set; } = string.Empty;
        public string Subtitle { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        public decimal Cost { get; set; }
        public decimal Profit { get; set; }
        public decimal MarginAtPurchaseRate { get; set; }
        public decimal MarginAtCurrentRate { get; set; }
        public decimal CurrencyMovementImpact { get; set; }
        public decimal PurchaseRateToBase { get; set; }
        public decimal CurrentRateToBase { get; set; }
        public bool CurrencyMovementEatsProfit { get; set; }
        public string CurrencyWarning { get; set; } = string.Empty;
    }
}
