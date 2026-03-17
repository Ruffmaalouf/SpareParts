namespace SpareParts.Domain.Sales
{
    // ── Line item ─────────────────────────────────────────────────────────────
    public class SaleItemDto
    {
        public int     PartId         { get; set; }
        public int     Quantity       { get; set; }
        public decimal UnitPrice      { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TaxRate        { get; set; }
    }

    // ── Create ────────────────────────────────────────────────────────────────
    public class CreateSaleRequest
    {
        public DateTime           InvoiceDate   { get; set; }
        public int?               CustomerId    { get; set; }
        public int                WarehouseId   { get; set; }
        public string?            PaymentMethod { get; set; }
        public decimal            PaidAmount    { get; set; }
        public string?            Notes         { get; set; }
        public List<SaleItemDto>  Items         { get; set; } = new();
    }

    public class CreateSaleResponse
    {
        public int     InvoiceId     { get; set; }
        public string  InvoiceNumber { get; set; } = string.Empty;
        public decimal TotalAmount   { get; set; }
        public string  PaymentStatus { get; set; } = string.Empty;
    }
}
