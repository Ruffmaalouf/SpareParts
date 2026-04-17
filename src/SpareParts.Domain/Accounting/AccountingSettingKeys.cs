namespace SpareParts.Domain.Accounting
{
    public static class AccountingSettingKeys
    {
        public const string SalesCash = "sales_cash";
        public const string SalesRevenue = "sales_revenue";
        public const string Cogs = "cogs";
        public const string Inventory = "inventory";
        public const string PurchaseOffset = "purchase_offset";

        public static readonly string[] All =
        {
            SalesCash,
            SalesRevenue,
            Cogs,
            Inventory,
            PurchaseOffset
        };
    }
}
