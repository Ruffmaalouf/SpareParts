namespace SpareParts.Infrastructure.Interfaces
{
    public sealed record SalesTotalsResult(decimal Subtotal, decimal DiscountTotal, decimal TaxTotal)
    {
        public decimal TotalAmount => Subtotal - DiscountTotal + TaxTotal;
    }
}
