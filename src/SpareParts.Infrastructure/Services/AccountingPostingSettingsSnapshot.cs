namespace SpareParts.Infrastructure.Services
{
    public sealed class AccountingPostingSettingsSnapshot
    {
        public int SalesCashAccountId { get; set; }
        public int SalesRevenueAccountId { get; set; }
        public int CogsAccountId { get; set; }
        public int InventoryAccountId { get; set; }
        public int PurchaseOffsetAccountId { get; set; }
    }
}
