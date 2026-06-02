using SpareParts.Domain.Purchases;
using SpareParts.Domain.Sales;
using SpareParts.Infrastructure.Interfaces;

namespace SpareParts.Infrastructure.Services
{
    

    public class InvoiceTotalsCalculator : IInvoiceTotalsCalculator
    {
        public SalesTotalsResult CalculateSales(IList<SaleItemDto> items)
        {
            decimal subtotal = 0;
            decimal discountTotal = 0;
            decimal taxTotal = 0;

            foreach (var item in items)
            {
                var baseLine  = Round2(item.Quantity * item.UnitPrice);
                var discount  = Round2(item.DiscountAmount);
                var net       = baseLine - discount;
                var tax       = Round2(net * (item.TaxRate / 100m));

                subtotal      += baseLine;
                discountTotal += discount;
                taxTotal      += tax;
            }

            return new SalesTotalsResult(subtotal, discountTotal, taxTotal);
        }

        public PurchaseTotalsResult CalculatePurchase(IList<PurchaseItemDto> items)
        {
            decimal subtotal = 0;
            decimal taxTotal = 0;

            foreach (var item in items)
            {
                var baseLine = Round2(item.Quantity * item.UnitCost);
                var tax      = Round2(baseLine * (item.TaxRate / 100m));

                subtotal += baseLine;
                taxTotal += tax;
            }

            return new PurchaseTotalsResult(subtotal, 0m, taxTotal);
        }

        private static decimal Round2(decimal value)
            => decimal.Round(value, 2, MidpointRounding.AwayFromZero);
    }
}
