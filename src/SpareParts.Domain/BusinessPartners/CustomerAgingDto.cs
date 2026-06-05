namespace SpareParts.Domain.BusinessPartners
{
    public class CustomerAgingDto
    {
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public decimal Current { get; set; }
        public decimal Days1To30 { get; set; }
        public decimal Days31To60 { get; set; }
        public decimal Days61To90 { get; set; }
        public decimal Over90Days { get; set; }
        public decimal TotalBalance { get; set; }
    }
}
