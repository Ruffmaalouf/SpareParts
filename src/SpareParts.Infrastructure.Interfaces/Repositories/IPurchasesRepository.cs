using SpareParts.Domain.Purchases;

namespace SpareParts.Infrastructure.Interfaces.Repositories
{
    public interface IPurchasesRepository
    {
        int InsertInvoice(PurchaseInvoice invoice);
        void InsertItems(int purchaseId, IList<PurchaseInvoiceItem> items);
        bool PurchaseNumberExists(string purchaseNumber);
    }
}
