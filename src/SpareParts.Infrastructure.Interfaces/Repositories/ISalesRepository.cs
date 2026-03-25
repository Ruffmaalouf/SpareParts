using SpareParts.Domain.Sales;

namespace SpareParts.Infrastructure.Interfaces.Repositories
{
    public interface ISalesRepository
    {
        int InsertInvoice(SalesInvoice invoice);
        void InsertItems(int invoiceId, IList<SalesInvoiceItem> items);
        bool InvoiceNumberExists(string invoiceNumber);
    }
}
