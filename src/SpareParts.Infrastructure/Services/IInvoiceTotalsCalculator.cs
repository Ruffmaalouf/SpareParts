using SpareParts.Domain.Purchases;
using SpareParts.Domain.Sales;
using SpareParts.Infrastructure.Interfaces;

namespace SpareParts.Infrastructure.Services
{
    public sealed record SalesTotalsResult(decimal Subtotal, decimal DiscountTotal, decimal TaxTotal)
    {
        public decimal TotalAmount => Subtotal - DiscountTotal + TaxTotal;
    }

    public sealed record PurchaseTotalsResult(decimal Subtotal, decimal DiscountTotal, decimal TaxTotal)
    {
        public decimal TotalAmount => Subtotal - DiscountTotal + TaxTotal;
    }

    public class InvoiceTotalsCalculator : IInvoiceTotalsCalculator
    {
        public SalesTotalsResult CalculateSales(IList<SaleItemDto> items)
        {
            decimal subtotal = 0;
            decimal discountTotal = 0;
            decimal taxTotal = 0;

            foreach (var item in items)
            {
                var baseLine = item.Quantity * item.UnitPrice;
                var net = baseLine - item.DiscountAmount;
                var tax = net * (item.TaxRate / 100m);

                subtotal += baseLine;
                discountTotal += item.DiscountAmount;
                taxTotal += tax;
            }

            return new SalesTotalsResult(subtotal, discountTotal, taxTotal);
        }

        public PurchaseTotalsResult CalculatePurchase(IList<PurchaseItemDto> items)
        {
            decimal subtotal = 0;
            decimal discountTotal = 0;
            decimal taxTotal = 0;

            foreach (var item in items)
            {
                var baseLine = item.Quantity * item.UnitCost;
                var tax = baseLine * (item.TaxRate / 100m);

                subtotal += baseLine;
                taxTotal += tax;
            }

            return new PurchaseTotalsResult(subtotal, discountTotal, taxTotal);
        }
    }
}
