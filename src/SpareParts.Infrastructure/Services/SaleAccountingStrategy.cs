using SpareParts.Domain.Accounting;
using SpareParts.Domain.Sales;

namespace SpareParts.Infrastructure.Services
{
    public class SaleAccountingStrategy : IAccountingStrategy<SalesInvoice>
    {
        private readonly int _cashAccountId;
        private readonly int _salesAccountId;
        private readonly int _cogsAccountId;
        private readonly int _inventoryAccountId;

        public SaleAccountingStrategy(int cashAccountId, int salesAccountId, int cogsAccountId, int inventoryAccountId)
        {
            _cashAccountId = cashAccountId;
            _salesAccountId = salesAccountId;
            _cogsAccountId = cogsAccountId;
            _inventoryAccountId = inventoryAccountId;
        }

        public List<JournalLine> BuildJournalLines(SalesInvoice invoice, int userId)
        {
            if (invoice.TotalAmount < 0 || invoice.TotalCost < 0)
            {
                throw new InvalidOperationException("Sale journal lines cannot be generated from negative totals.");
            }

            var lines = new List<JournalLine>
            {
                new() { AccountId = _cashAccountId, Debit = invoice.TotalAmount, Credit = 0, CreatedAt = DateTime.UtcNow, CreatedByUserId = userId },
                new() { AccountId = _salesAccountId, Debit = 0, Credit = invoice.TotalAmount, CreatedAt = DateTime.UtcNow, CreatedByUserId = userId },
                new() { AccountId = _cogsAccountId, Debit = invoice.TotalCost, Credit = 0, CreatedAt = DateTime.UtcNow, CreatedByUserId = userId },
                new() { AccountId = _inventoryAccountId, Debit = 0, Credit = invoice.TotalCost, CreatedAt = DateTime.UtcNow, CreatedByUserId = userId }
            };

            var totalDebit = lines.Sum(x => x.Debit);
            var totalCredit = lines.Sum(x => x.Credit);
            if (totalDebit != totalCredit)
            {
                throw new InvalidOperationException("Sale journal entry is not balanced.");
            }

            return lines;
        }
    }
}
