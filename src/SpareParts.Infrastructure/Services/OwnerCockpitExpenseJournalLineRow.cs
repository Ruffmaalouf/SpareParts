using Dapper;
using SpareParts.Domain.Accounting;
using SpareParts.Domain.OwnerCockpit;
using SpareParts.Domain.Transactions;
using SpareParts.Infrastructure.Data;
using SpareParts.Infrastructure.Interfaces;

namespace SpareParts.Infrastructure.Services
{
    internal sealed class OwnerCockpitExpenseJournalLineRow
    {
        public int JournalEntryId { get; set; }
        public string AccountCode { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }
}
