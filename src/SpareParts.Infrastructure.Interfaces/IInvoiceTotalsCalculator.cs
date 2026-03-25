using SpareParts.Domain.Purchases;
using SpareParts.Domain.Sales; 

namespace SpareParts.Infrastructure.Interfaces
{
    public interface IInvoiceTotalsCalculator
    {
        SalesTotalsResult CalculateSales(IList<SaleItemDto> items);
        PurchaseTotalsResult CalculatePurchase(IList<PurchaseItemDto> items);
    }
    public sealed record SalesTotalsResult(decimal Subtotal, decimal DiscountTotal, decimal TaxTotal)
    {
        public decimal TotalAmount => Subtotal - DiscountTotal + TaxTotal;
    }

    public sealed record PurchaseTotalsResult(decimal Subtotal, decimal DiscountTotal, decimal TaxTotal)
    {
        public decimal TotalAmount => Subtotal - DiscountTotal + TaxTotal;
    }
}
