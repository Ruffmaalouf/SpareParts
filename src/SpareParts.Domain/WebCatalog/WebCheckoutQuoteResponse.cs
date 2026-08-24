namespace SpareParts.Domain.WebCatalog
{
    /// <summary>
    /// Response for POST /api/web-catalog/checkout/quote — a read-only price preview. No invoice is
    /// created and no stock is touched; these are the same numbers Checkout would produce for the
    /// same cart and promo code.
    /// </summary>
    public sealed class WebCheckoutQuoteResponse
    {
        public decimal Subtotal { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal DiscountPercent { get; set; }
        public decimal TotalAmount { get; set; }
    }
}
