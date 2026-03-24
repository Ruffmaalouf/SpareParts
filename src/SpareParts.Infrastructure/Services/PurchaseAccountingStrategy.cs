using SpareParts.Domain.Accounting;
using SpareParts.Domain.Purchases;

namespace SpareParts.Infrastructure.Services
{
    public class PurchaseAccountingStrategy : IAccountingStrategy<PurchaseInvoice>
    {
        private readonly int _inventoryAccountId;
        private readonly int _cashOrApAccountId;

        public PurchaseAccountingStrategy(int inventoryAccountId, int cashOrApAccountId)
        {
            _inventoryAccountId = inventoryAccountId;
            _cashOrApAccountId = cashOrApAccountId;
        }

        public List<JournalLine> BuildJournalLines(PurchaseInvoice purchase, int userId)
        {
            if (purchase.TotalAmount < 0)
            {
                throw new InvalidOperationException("Purchase journal lines cannot be generated from negative totals.");
            }

            var lines = new List<JournalLine>
            {
                new() { AccountId = _inventoryAccountId, Debit = purchase.TotalAmount, Credit = 0, CreatedAt = DateTime.UtcNow, CreatedByUserId = userId },
                new() { AccountId = _cashOrApAccountId, Debit = 0, Credit = purchase.TotalAmount, CreatedAt = DateTime.UtcNow, CreatedByUserId = userId }
            };

            var totalDebit = lines.Sum(x => x.Debit);
            var totalCredit = lines.Sum(x => x.Credit);
            if (totalDebit != totalCredit)
            {
                throw new InvalidOperationException("Purchase journal entry is not balanced.");
            }

            return lines;
        }
    }
}
