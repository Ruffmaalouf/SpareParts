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
            var lines = new List<JournalLine>();

            lines.Add(new JournalLine { AccountId = _inventoryAccountId, Debit = purchase.TotalAmount, Credit = 0, CreatedAt = DateTime.UtcNow, CreatedByUserId = userId });
            lines.Add(new JournalLine { AccountId = _cashOrApAccountId, Debit = 0, Credit = purchase.TotalAmount, CreatedAt = DateTime.UtcNow, CreatedByUserId = userId });

            return lines;
        }
    }
}
