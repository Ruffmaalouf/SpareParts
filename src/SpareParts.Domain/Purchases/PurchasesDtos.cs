namespace SpareParts.Domain.Purchases
{
    // ── Line item ─────────────────────────────────────────────────────────────
    public class PurchaseItemDto
    {
        public int     PartId    { get; set; }
        public int     Quantity  { get; set; }
        public decimal UnitCost  { get; set; }
        public decimal TaxRate   { get; set; }
    }

    // ── Create ────────────────────────────────────────────────────────────────
    public class CreatePurchaseRequest
    {
        public DateTime               PurchaseDate   { get; set; }
        public int                    SupplierId     { get; set; }
        public int                    WarehouseId    { get; set; }
        public decimal                PaidAmount     { get; set; }
        public string                 PaymentStatus  { get; set; } = "Unpaid";
        public List<PurchaseItemDto>  Items          { get; set; } = new();
    }

    public class CreatePurchaseResponse
    {
        public int     PurchaseId     { get; set; }
        public string  PurchaseNumber { get; set; } = string.Empty;
        public decimal TotalAmount    { get; set; }
        public string  PaymentStatus  { get; set; } = string.Empty;
    }
}
