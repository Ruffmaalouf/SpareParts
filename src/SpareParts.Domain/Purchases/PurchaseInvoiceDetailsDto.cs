using SpareParts.Domain.Transactions;

namespace SpareParts.Domain.Purchases
{
    public sealed class PurchaseInvoiceDetailsDto
    {
        public int PurchaseId { get; set; }
        public string PurchaseNumber { get; set; } = string.Empty;
        public string? ScanCode { get; set; }
        public DateTime PurchaseDate { get; set; }
        public int SupplierId { get; set; }
        public int WarehouseId { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public List<PurchaseInvoiceLineDto> Items { get; set; } = new();
        public List<TransactionTimelineStepDto> Timeline { get; set; } = new();
    }
}
