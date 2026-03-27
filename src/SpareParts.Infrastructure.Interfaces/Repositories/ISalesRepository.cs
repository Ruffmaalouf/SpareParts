using SpareParts.Domain.Sales;
using System.Collections.Generic;

namespace SpareParts.Infrastructure.Interfaces.Repositories
{
    public interface ISalesRepository
    {
        int InsertInvoice(SalesInvoice invoice);
        void InsertItems(int invoiceId, IList<SalesInvoiceItem> items);
        bool InvoiceNumberExists(string invoiceNumber);
        List<SalesInvoiceLookupDto> SearchInvoices(string? query);
        SalesInvoiceDetailsDto? GetInvoiceById(int invoiceId);
        bool UpdateInvoice(int invoiceId, UpdateSaleRequest request, int userId);
    }
}
