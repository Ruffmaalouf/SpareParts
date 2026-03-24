namespace SpareParts.Infrastructure.Services
{
    public class AccountingOptions
    {
        public int CashAccountId { get; set; } = 1;
        public int SalesAccountId { get; set; } = 5;
        public int CogsAccountId { get; set; } = 6;
        public int InventoryAccountId { get; set; } = 2;
        public int CashOrApAccountId { get; set; } = 4;
    }
}
