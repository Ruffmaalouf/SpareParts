namespace SpareParts.Domain.Accounting
{
    public static class AccountingSettingKeys
    {
        public const string SalesCash = "sales_cash";
        public const string SalesRevenue = "sales_revenue";
        public const string Cogs = "cogs";
        public const string Inventory = "inventory";
        public const string PurchaseOffset = "purchase_offset";
        public const string UsedCarPrice = "used_car_price";
        public const string UsedCarTransportation = "used_car_transportation";
        public const string UsedCarPartOut = "used_car_partout";
        public const string UsedCarShipping = "used_car_shipping";
        public const string UsedCarCustoms = "used_car_customs";
        public const string UsedCarRepairs = "used_car_repairs";

        public static readonly string[] All =
        {
            SalesCash,
            SalesRevenue,
            Cogs,
            Inventory,
            PurchaseOffset,
            UsedCarPrice,
            UsedCarTransportation,
            UsedCarPartOut,
            UsedCarShipping,
            UsedCarCustoms,
            UsedCarRepairs
        };
    }
}
