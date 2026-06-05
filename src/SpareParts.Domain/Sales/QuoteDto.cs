namespace SpareParts.Domain.Sales
{
    public class QuoteLookupDto
    {
        public int Id { get; set; }
        public string QuoteNumber { get; set; } = string.Empty;
        public DateTime QuoteDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public int? CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string? CustomerPhone { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class QuoteDetailsDto
    {
        public int Id { get; set; }
        public string QuoteNumber { get; set; } = string.Empty;
        public DateTime QuoteDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public int? CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string? CustomerPhone { get; set; }
        public int? WarehouseId { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<QuoteLineDto> Items { get; set; } = new();
        public decimal TotalAmount => Items.Sum(i => i.LineTotal);
    }

    public class QuoteLineDto
    {
        public int Id { get; set; }
        public int? PartId { get; set; }
        public string Description { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal LineTotal => decimal.Round(Quantity * UnitPrice - DiscountAmount, 4, MidpointRounding.AwayFromZero);
        public int SortOrder { get; set; }
    }

    public class CreateQuoteRequest
    {
        public DateTime QuoteDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public int? CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string? CustomerPhone { get; set; }
        public int? WarehouseId { get; set; }
        public string? Notes { get; set; }
        public List<CreateQuoteLineRequest> Items { get; set; } = new();
    }

    public class CreateQuoteLineRequest
    {
        public int? PartId { get; set; }
        public string Description { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal DiscountAmount { get; set; }
    }

    public class UpdateQuoteStatusRequest
    {
        public string Status { get; set; } = string.Empty;
    }

    public class ConvertQuoteToInvoiceResponse
    {
        public int InvoiceId { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
    }
}
