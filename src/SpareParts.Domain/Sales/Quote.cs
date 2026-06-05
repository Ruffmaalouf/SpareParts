namespace SpareParts.Domain.Sales
{
    public class Quote
    {
        public int Id { get; set; }
        public string QuoteNumber { get; set; } = string.Empty;
        public DateTime QuoteDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public int? CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string? CustomerPhone { get; set; }
        public int? WarehouseId { get; set; }
        public string Status { get; set; } = "Draft";
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? CreatedByUserId { get; set; }
        public List<QuoteItem> Items { get; set; } = new();
    }

    public class QuoteItem
    {
        public int Id { get; set; }
        public int QuoteId { get; set; }
        public int? PartId { get; set; }
        public string Description { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal DiscountAmount { get; set; }
        public int SortOrder { get; set; }
    }
}
