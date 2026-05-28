namespace SpareParts.Domain.OwnerCockpit
{
    public sealed class OwnerCockpitDailyProfitLossDto
    {
        public DateTime BusinessDate { get; set; }
        public string CurrencyCode { get; set; } = "USD";
        public decimal GrossSales { get; set; }
        public decimal CostOfGoodsSold { get; set; }
        public decimal GrossProfit { get; set; }
        public decimal RentExpense { get; set; }
        public decimal LaborExpense { get; set; }
        public decimal OtherOperatingExpenses { get; set; }
        public decimal TotalOperatingExpenses { get; set; }
        public decimal NetProfitLoss { get; set; }
        public List<OwnerCockpitExpenseBreakdownRowDto> ExpenseBreakdown { get; set; } = new();
    }
}
