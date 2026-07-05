namespace SpareParts.Domain.Clients
{
    /// <summary>
    /// One received payment for the workspace Payments tab — sourced from the
    /// sale-payment journal entries the existing payment flows already write.
    /// </summary>
    public sealed class ClientPaymentDto
    {
        public int JournalEntryId { get; set; }
        public DateTime PaymentDate { get; set; }
        public decimal Amount { get; set; }
        public int? InvoiceId { get; set; }
        public string? InvoiceNumber { get; set; }
        public string? Description { get; set; }
    }
}
