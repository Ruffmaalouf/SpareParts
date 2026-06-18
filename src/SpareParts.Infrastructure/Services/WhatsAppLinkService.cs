using Dapper;
using SpareParts.Domain.Marketplace;
using SpareParts.Infrastructure.Data;
using SpareParts.Infrastructure.Interfaces;

namespace SpareParts.Infrastructure.Services;

public sealed class WhatsAppLinkService
{
    private readonly ISqlConnectionFactory _factory;

    public WhatsAppLinkService(ISqlConnectionFactory factory)
    {
        _factory = factory;
    }

    public string GenerateLink(string phoneNumber, string message)
    {
        var cleanPhone = new string(phoneNumber.Where(char.IsDigit).ToArray());
        return $"https://wa.me/{cleanPhone}?text={Uri.EscapeDataString(message)}";
    }

    public (string Link, string Message) GeneratePartLink(int partId, string? sellerPhone, int tenantId)
    {
        using var connection = _factory.CreateConnection();

        var part = connection.QuerySingleOrDefault<WhatsAppPartRow>(
            """
SELECT Name, InternalCode, SalePrice, Currency
FROM dbo.Parts
WHERE Id = @PartId AND (@TenantId = 0 OR TenantId = @TenantId);
""",
            new { PartId = partId, TenantId = tenantId });

        if (part is null)
            throw new NotFoundException("Part not found.");

        var message = $"Hi, I saw your listing: {part.Name} ({part.InternalCode}), Price: {part.SalePrice} {part.Currency}. Is it still available?";
        var link = GenerateLink(sellerPhone ?? string.Empty, message);
        return (link, message);
    }

    public IEnumerable<WhatsAppCatalogItemDto> ExportCatalog(int tenantId)
    {
        using var connection = _factory.CreateConnection();

        return connection.Query<WhatsAppCatalogItemDto>(
            """
SELECT
    p.InternalCode AS ProductId,
    p.Name,
    ISNULL(p.Notes, '') AS Description,
    p.SalePrice AS Price,
    p.Currency,
    CASE
        WHEN ISNULL(s.AvailableQty, 0) > 0 THEN 'in stock'
        ELSE 'out of stock'
    END AS Availability,
    p.PhotoUrl AS ImageUrl,
    p.Barcode,
    c.Name AS Category,
    '' AS ListingUrl
FROM dbo.Parts p
LEFT JOIN dbo.Categories c ON c.Id = p.CategoryId
OUTER APPLY (
    SELECT SUM(Quantity - ISNULL(ReservedQuantity, 0)) AS AvailableQty
    FROM dbo.Stock
    WHERE PartId = p.Id
) s
WHERE p.IsActive = 1
  AND (@TenantId = 0 OR p.TenantId = @TenantId)
ORDER BY p.Name;
""",
            new { TenantId = tenantId })
            .ToList();
    }

}
