namespace SpareParts.Infrastructure.Services;

/// <summary>
/// Single source of truth for the public web storefront's promo code. WebCatalogService.Checkout
/// reads only these three values — to change the code, its discount, or turn it off entirely, edit
/// this file and nothing else in the codebase needs to change.
/// </summary>
public static class PromoCodeSettings
{
    /// <summary>Set to false to reject every promo code (checkout still works, just without a discount).</summary>
    public const bool IsEnabled = true;

    /// <summary>Matched case-insensitively, after trimming whitespace, against the code the shopper enters.</summary>
    public const string Code = "WEB10";

    /// <summary>Fraction of the order subtotal deducted when the code above is applied. 0.10m = 10%.</summary>
    public const decimal DiscountRate = 0.10m;
}
