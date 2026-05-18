namespace SpareParts.Domain.OwnerCockpit
{
    public sealed class OwnerCockpitUnpaidTransactionDto
    {
        public string TransactionType { get; set; } = string.Empty;
        public string TransactionNumber { get; set; } = string.Empty;
        public DateTime TransactionDate { get; set; }
        public string Counterparty { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal RemainingAmount { get; set; }
        public string PaymentStatus { get; set; } = string.Empty;
    }
}
