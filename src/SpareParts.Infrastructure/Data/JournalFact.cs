using Dapper;
using SpareParts.Domain.Transactions;

namespace SpareParts.Infrastructure.Data
{
    internal sealed class JournalFact
    {
        public int JournalEntryId { get; set; }
        public string ReferenceType { get; set; } = string.Empty;
        public int? ReferenceId { get; set; }
        public DateTime EntryDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Description { get; set; } = string.Empty;
        public int AccountId { get; set; }
        public string AccountCode { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
    }
}
