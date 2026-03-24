namespace SpareParts.Domain.Sales
{
    public class CreateSaleRequest
    {
        public DateTime InvoiceDate { get; set; }
        public int? CustomerId { get; set; }
        public int WarehouseId { get; set; }
        public string? PaymentMethod { get; set; }
        public decimal PaidAmount { get; set; }
        public string? Notes { get; set; }
        public List<SaleItemDto> Items { get; set; } = new();
    }
}
