namespace SpareParts.Infrastructure.Interfaces
{
    public sealed record PurchaseTotalsResult(decimal Subtotal, decimal DiscountTotal, decimal TaxTotal)
    {
        public decimal TotalAmount => Subtotal - DiscountTotal + TaxTotal;
    }
}
