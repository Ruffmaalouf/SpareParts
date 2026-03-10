using SpareParts.Domain.Accounting;
using SpareParts.Domain.Sales;
using SpareParts.Domain.Purchases;

namespace SpareParts.Infrastructure.Services
{
    public interface IAccountingStrategy<TDocument>
    {
        List<JournalLine> BuildJournalLines(TDocument doc, int userId);
    }

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
            var lines = new List<JournalLine>();

            lines.Add(new JournalLine
            {
                AccountId = _cashAccountId,
                Debit = invoice.TotalAmount,
                Credit = 0,
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = userId
            });

            lines.Add(new JournalLine
            {
                AccountId = _salesAccountId,
                Debit = 0,
                Credit = invoice.TotalAmount,
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = userId
            });

            lines.Add(new JournalLine
            {
                AccountId = _cogsAccountId,
                Debit = invoice.TotalCost,
                Credit = 0,
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = userId
            });

            lines.Add(new JournalLine
            {
                AccountId = _inventoryAccountId,
                Debit = 0,
                Credit = invoice.TotalCost,
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = userId
            });

            return lines;
        }
    }

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

            // Debit Inventory (increase asset)
            lines.Add(new JournalLine
            {
                AccountId = _inventoryAccountId,
                Debit = purchase.TotalAmount,
                Credit = 0,
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = userId
            });

            // Credit Cash/AP (depending how you treat it, but here it's generic)
            lines.Add(new JournalLine
            {
                AccountId = _cashOrApAccountId,
                Debit = 0,
                Credit = purchase.TotalAmount,
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = userId
            });

            return lines;
        }
    }
}
