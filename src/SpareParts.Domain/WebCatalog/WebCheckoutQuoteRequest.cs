using System.ComponentModel.DataAnnotations;

namespace SpareParts.Domain.WebCatalog
{
    /// <summary>
    /// Request for POST /api/web-catalog/checkout/quote — prices a cart and validates a promo code
    /// without creating an order. Same item shape as <see cref="WebCheckoutRequest"/>.
    /// </summary>
    public sealed class WebCheckoutQuoteRequest
    {
        /// <summary>Optional promo code (e.g. "WEB10"). Discount is always computed server-side —
        /// never send a discount amount from the browser.</summary>
        [MaxLength(40)]
        public string? PromoCode { get; set; }

        [MinLength(1)]
        public List<WebCheckoutItemDto> Items { get; set; } = new();
    }
}
