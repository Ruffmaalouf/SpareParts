namespace SpareParts.Domain.Sales
{
    public class SalesInvoiceLineDto
    {
        public int PartId { get; set; }
        public string Description { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal CostPrice { get; set; }
        public decimal LineRevenue => decimal.Round(Quantity * UnitPrice, 4, MidpointRounding.AwayFromZero);
        public decimal LineCost => decimal.Round(Quantity * CostPrice, 4, MidpointRounding.AwayFromZero);
        public decimal LineProfit => LineRevenue - LineCost;
        public decimal ProfitMarginPercent => LineRevenue > 0
            ? decimal.Round(LineProfit / LineRevenue * 100, 2, MidpointRounding.AwayFromZero)
            : 0m;
    }
}
