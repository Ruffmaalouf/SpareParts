namespace SpareParts.Domain.Sales
{
    /// <summary>
    /// A sales invoice overdue by more than the AR Collections Agent's grace period, with a real
    /// customer phone on file, that hasn't been reminded within the cooldown window.
    /// </summary>
    public sealed class OverdueInvoiceDto
    {
        /// <summary>The underlying <c>dbo.Transactions.Id</c> row, used to mark the reminder sent.</summary>
        public int TransactionId { get; set; }

        /// <summary>The invoice number/reference (<c>dbo.Transactions.ReferenceId</c>) the communications template expects.</summary>
        public int InvoiceId { get; set; }

        public string CustomerName { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public decimal Balance { get; set; }
        public DateTime TransactionDate { get; set; }
        public int CreatedByUserId { get; set; }
    }
}
