using Dapper;
using SpareParts.Domain.Transactions;

namespace SpareParts.Infrastructure.Data
{
    internal sealed class TransactionTimelineSnapshot
    {
        public string TypeKey { get; set; } = string.Empty;
        public int ReferenceId { get; set; }
        public string TransactionNumber { get; set; } = string.Empty;
        public DateTime TransactionDate { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public string PaymentStatus { get; set; } = string.Empty;
        public string PostingStatus { get; set; } = string.Empty;
        public DateTime? PostedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ModifiedAt { get; set; }
        public int? UsedCarId { get; set; }
        public DateTime? ReceivedAt { get; set; }
    }
}
