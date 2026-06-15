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
        bool UpdateInvoice(int invoiceId, SalesInvoice invoice, IList<SalesInvoiceItem> items, int userId);
        bool ApplyPayment(int invoiceId, decimal paidAmount, string paymentStatus, string? paymentMethod, int userId);
        List<SalesProfitHistoryPointDto> GetProfitHistory(int months);
    }
}
