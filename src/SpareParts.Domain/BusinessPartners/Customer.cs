using SpareParts.Domain.Common;

namespace SpareParts.Domain.BusinessPartners
{
    public class Customer : AuditableEntity
    {
        public string Name { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public string? TaxNumber { get; set; }
        public decimal OpeningBalance { get; set; }
        public decimal CreditLimit { get; set; }
        public int? AccountId { get; set; }
    }
}
