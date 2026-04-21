using SpareParts.Domain.Purchases;

namespace SpareParts.Infrastructure.Interfaces.Repositories
{
    public interface IPurchasesRepository
    {
        int InsertInvoice(PurchaseInvoice invoice);
        void InsertItems(int purchaseId, IList<PurchaseInvoiceItem> items);
        bool PurchaseNumberExists(string purchaseNumber);
        List<PurchaseInvoiceLookupDto> SearchPurchases(string? query);
        PurchaseInvoiceDetailsDto? GetInvoiceById(int purchaseId);
        bool UpdateInvoice(int purchaseId, PurchaseInvoice invoice, IList<PurchaseInvoiceItem> items, int userId);
    }
}
