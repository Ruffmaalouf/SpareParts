using Dapper;
using SpareParts.Domain.Accounting;
using SpareParts.Domain.OwnerCockpit;
using SpareParts.Domain.Transactions;
using SpareParts.Infrastructure.Data;
using SpareParts.Infrastructure.Interfaces;

namespace SpareParts.Infrastructure.Services
{
    internal sealed class OwnerCockpitSummaryRow
    {
        public int TodaySalesCount { get; set; }
        public decimal TodaySalesAmount { get; set; }
        public decimal TodaySalesPaidAmount { get; set; }
        public decimal TodaySalesProfit { get; set; }
        public int TodayPurchasesCount { get; set; }
        public decimal TodayPurchasesAmount { get; set; }
        public decimal TodayPurchasesPaidAmount { get; set; }
        public decimal CustomerDebt { get; set; }
        public decimal SupplierDebt { get; set; }
        public decimal StockValue { get; set; }
    }
}
