namespace SpareParts.Domain.Sales
{
    public class SalesReturnDetailsDto
    {
        public int ReturnId { get; set; }
        public string ReturnNumber { get; set; } = string.Empty;
        public int OriginalInvoiceId { get; set; }
        public string OriginalInvoiceNumber { get; set; } = string.Empty;
        public DateTime ReturnDate { get; set; }
        public int? CustomerId { get; set; }
        public int WarehouseId { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal RefundAmount { get; set; }
        public string? Reason { get; set; }
        public List<SalesReturnLineDto> Items { get; set; } = new();
    }

    public class SalesReturnLineDto
    {
        public int PartId { get; set; }
        public string Description { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TaxRate { get; set; }
        public decimal LineTotal { get; set; }
    }

    public class ReturnableLineDto
    {
        public int PartId { get; set; }
        public string Description { get; set; } = string.Empty;
        public int OriginalQuantity { get; set; }
        public int AlreadyReturnedQuantity { get; set; }
        public int ReturnableQuantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TaxRate { get; set; }
    }
}
