namespace SpareParts.Domain.Sales
{
    public class CreateSalesReturnRequest
    {
        public int OriginalInvoiceId { get; set; }
        public DateTime ReturnDate { get; set; }
        public int WarehouseId { get; set; }
        public string? Reason { get; set; }
        public decimal RefundAmount { get; set; }
        public List<SalesReturnItemDto> Items { get; set; } = new();
    }

    public class SalesReturnItemDto
    {
        public int PartId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TaxRate { get; set; }
    }
}
