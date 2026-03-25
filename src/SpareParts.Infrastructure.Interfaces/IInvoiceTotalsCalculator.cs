using SpareParts.Domain.Purchases;
using SpareParts.Domain.Sales;
using SpareParts.Infrastructure.Services;

namespace SpareParts.Infrastructure.Interfaces
{
    public interface IInvoiceTotalsCalculator
    {
        SalesTotalsResult CalculateSales(IList<SaleItemDto> items);
        PurchaseTotalsResult CalculatePurchase(IList<PurchaseItemDto> items);
    }
}
