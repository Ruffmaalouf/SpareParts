using SpareParts.Domain.Transactions;

namespace SpareParts.Domain.Sales
{
    public class SalesInvoiceDetailsDto
    {
        public int InvoiceId { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public string? ScanCode { get; set; }
        public DateTime InvoiceDate { get; set; }
        public int? CustomerId { get; set; }
        public int WarehouseId { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public string BaseCurrencyCode { get; set; } = string.Empty;
        public string CounterCurrencyCode { get; set; } = string.Empty;
        public decimal CounterRateToBase { get; set; }
        public List<SalesInvoiceLineDto> Items { get; set; } = new();
        public List<TransactionTimelineStepDto> Timeline { get; set; } = new();
    }
}
