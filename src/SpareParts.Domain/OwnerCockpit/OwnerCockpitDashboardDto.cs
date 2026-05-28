namespace SpareParts.Domain.OwnerCockpit
{
    public sealed class OwnerCockpitDashboardDto
    {
        public DateTime BusinessDate { get; set; }
        public DateTime GeneratedAt { get; set; }
        public string CurrencyCode { get; set; } = "USD";

        public int TodaySalesCount { get; set; }
        public decimal TodaySalesAmount { get; set; }
        public decimal TodaySalesPaidAmount { get; set; }
        public decimal TodaySalesProfit { get; set; }

        public int TodayPurchasesCount { get; set; }
        public decimal TodayPurchasesAmount { get; set; }
        public decimal TodayPurchasesPaidAmount { get; set; }

        public decimal CashBalance { get; set; }
        public decimal SupplierDebt { get; set; }
        public decimal CustomerDebt { get; set; }
        public decimal StockValue { get; set; }

        public decimal TotalCarProfit { get; set; }
        public decimal TotalPartProfit { get; set; }
        public int UnpaidTransactionCount { get; set; }
        public decimal UnpaidTransactionAmount { get; set; }
        public int AccountingAlertCount { get; set; }

        public OwnerCockpitDailyProfitLossDto DailyProfitLoss { get; set; } = new();
        public List<OwnerCockpitProfitRowDto> ProfitPerCar { get; set; } = new();
        public List<OwnerCockpitProfitRowDto> ProfitPerPart { get; set; } = new();
        public List<OwnerCockpitProfitHeatmapRowDto> ProfitHeatmap { get; set; } = new();
        public List<OwnerCockpitUnpaidTransactionDto> UnpaidTransactions { get; set; } = new();
        public List<OwnerCockpitAlertDto> AccountingAlerts { get; set; } = new();
    }
}
