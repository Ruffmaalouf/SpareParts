using SpareParts.Domain.Accounting;
using SpareParts.Domain.Inventory;
using SpareParts.Domain.Purchases;
using SpareParts.Domain.Sales;

namespace SpareParts.Infrastructure.Data
{
    public interface IPartsRepository
    {
        Dictionary<int, Part> GetByIds(IList<int> partIds);
    }

    public interface ISalesRepository
    {
        int InsertInvoice(SalesInvoice invoice);
        void InsertItems(int invoiceId, IList<SalesInvoiceItem> items);
        bool InvoiceNumberExists(string invoiceNumber);
    }

    public interface IPurchasesRepository
    {
        int InsertInvoice(PurchaseInvoice invoice);
        void InsertItems(int purchaseId, IList<PurchaseInvoiceItem> items);
        bool PurchaseNumberExists(string purchaseNumber);
    }

    public interface IJournalRepository
    {
        int InsertEntry(JournalEntry entry);
        void InsertLines(int entryId, IList<JournalLine> lines);
    }

    public class PartsRepository : IPartsRepository
    {
        private readonly SparePartsDataContext _ctx;

        public PartsRepository(SparePartsDataContext ctx)
        {
            _ctx = ctx;
        }

        public Dictionary<int, Part> GetByIds(IList<int> partIds)
        {
            return _ctx.GetPartsByIds(partIds).ToDictionary(p => p.Id, p => p);
        }
    }

    public class SalesRepository : ISalesRepository
    {
        private readonly SparePartsDataContext _ctx;

        public SalesRepository(SparePartsDataContext ctx)
        {
            _ctx = ctx;
        }

        public int InsertInvoice(SalesInvoice invoice) => _ctx.InsertSalesInvoice(invoice);

        public void InsertItems(int invoiceId, IList<SalesInvoiceItem> items) => _ctx.InsertSalesInvoiceItems(invoiceId, items);

        public bool InvoiceNumberExists(string invoiceNumber) => _ctx.SalesInvoiceNumberExists(invoiceNumber);
    }

    public class PurchasesRepository : IPurchasesRepository
    {
        private readonly SparePartsDataContext _ctx;

        public PurchasesRepository(SparePartsDataContext ctx)
        {
            _ctx = ctx;
        }

        public int InsertInvoice(PurchaseInvoice invoice) => _ctx.InsertPurchaseInvoice(invoice);

        public void InsertItems(int purchaseId, IList<PurchaseInvoiceItem> items) => _ctx.InsertPurchaseInvoiceItems(purchaseId, items);

        public bool PurchaseNumberExists(string purchaseNumber) => _ctx.PurchaseNumberExists(purchaseNumber);
    }

    public class JournalRepository : IJournalRepository
    {
        private readonly SparePartsDataContext _ctx;

        public JournalRepository(SparePartsDataContext ctx)
        {
            _ctx = ctx;
        }

        public int InsertEntry(JournalEntry entry) => _ctx.InsertJournalEntry(entry);

        public void InsertLines(int entryId, IList<JournalLine> lines) => _ctx.InsertJournalLines(entryId, lines);
    }
}
