using SpareParts.Domain.Common;

namespace SpareParts.Domain.Accounting
{
    public enum AccountType
    {
        Asset,
        Liability,
        Equity,
        Income,
        Expense
    }

    public class Account : AuditableEntity
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public AccountType AccountType { get; set; }
        public int? ParentId { get; set; }
    }
}
