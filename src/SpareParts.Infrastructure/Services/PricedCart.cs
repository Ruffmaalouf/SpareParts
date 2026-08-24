using SpareParts.Domain.Sales;

namespace SpareParts.Infrastructure.Services;

/// <summary>
/// Result of pricing a web-storefront cart (parts loaded, priced, validated against stock, and any
/// promo code applied) before anything is persisted. Shared by both WebCatalogService.Checkout
/// (which goes on to create a real invoice from <see cref="Items"/>) and WebCatalogService.Quote
/// (which only returns the numbers below) so the two can never disagree.
/// </summary>
internal sealed record PricedCart(
    IReadOnlyList<SaleItemDto> Items,
    decimal Subtotal,
    decimal DiscountAmount,
    decimal DiscountPercent,
    int TenantId,
    CheckoutWarehouse Warehouse);
