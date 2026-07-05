namespace SpareParts.Domain.Clients
{
    /// <summary>One quote row for the workspace Quotes tab (Report 04 §06).</summary>
    public sealed class ClientQuoteDto
    {
        public int QuoteId { get; set; }
        public string QuoteNumber { get; set; } = string.Empty;
        public DateTime QuoteDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
    }
}
