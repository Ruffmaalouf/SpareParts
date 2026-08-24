namespace SpareParts.Infrastructure.Services;

/// <summary>
/// Configures which tenant the anonymous public web storefront (GET /api/web-catalog/parts and the
/// web checkout flow) resolves to when a request has no authenticated tenant claim.
///
/// TenantResolutionMiddleware intentionally never assigns a tenant to unauthenticated requests
/// (login/browsing must not require a token), so ITenantContext.TenantId stays at its default of 0
/// for anonymous shoppers. Without this option, WebCatalogService's anonymous-tenant guard (added to
/// close the C1 cross-tenant leak) fails closed and the public catalog always returns zero parts.
///
/// Set <see cref="TenantId"/> to the single tenant whose catalog should be publicly browsable.
/// Leave it at 0 (unconfigured) to keep the storefront fully locked down (fail closed, no parts
/// returned to anonymous callers) — e.g. before a specific storefront tenant has been decided.
/// </summary>
public sealed class PublicCatalogOptions
{
    public int TenantId { get; init; }
}
