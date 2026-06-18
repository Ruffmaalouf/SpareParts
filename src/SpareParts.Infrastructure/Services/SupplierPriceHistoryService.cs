using Dapper;
using SpareParts.Domain.Purchases;
using SpareParts.Infrastructure.Data;
using SpareParts.Infrastructure.Interfaces;

namespace SpareParts.Infrastructure.Services;

public sealed class SupplierPriceHistoryService
{
    private readonly ISqlConnectionFactory _factory;
    private readonly ITenantContext _tenantContext;

    public SupplierPriceHistoryService(ISqlConnectionFactory factory, ITenantContext tenantContext)
    {
        _factory = factory;
        _tenantContext = tenantContext;
    }

    public IReadOnlyList<SupplierPriceHistoryDto> GetHistory(int partId)
    {
        using var session = new DbSession(_factory, _tenantContext.TenantId);
        return session.Connection.Query<SupplierPriceHistoryDto>(
            """
SELECT TOP 50
    sph.Id,
    sph.PartId,
    p.Name AS PartName,
    p.InternalCode AS PartCode,
    sph.SupplierId,
    s.Name AS SupplierName,
    sph.UnitPrice,
    sph.CurrencyCode,
    sph.Quantity,
    sph.InvoiceId,
    sph.RecordedAt
FROM dbo.SupplierPriceHistory sph
INNER JOIN dbo.Parts p ON p.Id = sph.PartId
INNER JOIN dbo.Suppliers s ON s.Id = sph.SupplierId
WHERE sph.PartId = @PartId
  AND (@TenantId = 0 OR sph.TenantId = @TenantId)
ORDER BY sph.RecordedAt DESC
""",
            new { PartId = partId, session.TenantId },
            session.Transaction).ToList();
    }

    public SupplierPriceComparisonDto? GetComparison(int partId)
    {
        using var session = new DbSession(_factory, _tenantContext.TenantId);

        var part = session.Connection.QueryFirstOrDefault<(string Name, string Code)>(
            """
SELECT Name, InternalCode AS Code FROM dbo.Parts
WHERE Id = @PartId AND IsActive = 1 AND (@TenantId = 0 OR TenantId = @TenantId)
""",
            new { PartId = partId, session.TenantId },
            session.Transaction);

        if (part == default)
            return null;

        var supplierPrices = session.Connection.Query<SupplierLastPriceDto>(
            """
SELECT
    s.Id AS SupplierId,
    s.Name AS SupplierName,
    MAX(sph.UnitPrice) AS LastPrice,
    MIN(sph.UnitPrice) AS LowestPrice,
    AVG(sph.UnitPrice) AS AveragePrice,
    COUNT(*) AS PurchaseCount,
    MAX(sph.RecordedAt) AS LastPurchaseDate
FROM dbo.SupplierPriceHistory sph
INNER JOIN dbo.Suppliers s ON s.Id = sph.SupplierId
WHERE sph.PartId = @PartId
  AND (@TenantId = 0 OR sph.TenantId = @TenantId)
GROUP BY s.Id, s.Name
ORDER BY LastPrice ASC
""",
            new { PartId = partId, session.TenantId },
            session.Transaction).ToList();

        // Correct LastPrice to be the most recent (not MAX)
        foreach (var sp in supplierPrices)
        {
            var lastPrice = session.Connection.ExecuteScalar<decimal>(
                """
SELECT TOP 1 UnitPrice
FROM dbo.SupplierPriceHistory
WHERE PartId = @PartId AND SupplierId = @SupplierId
  AND (@TenantId = 0 OR TenantId = @TenantId)
ORDER BY RecordedAt DESC
""",
                new { PartId = partId, SupplierId = sp.SupplierId, session.TenantId },
                session.Transaction);
            sp.LastPrice = lastPrice;
        }

        return new SupplierPriceComparisonDto
        {
            PartId = partId,
            PartName = part.Name,
            PartCode = part.Code,
            SupplierPrices = supplierPrices
        };
    }

    public void RecordPrice(RecordSupplierPriceRequest req, int userId)
    {
        using var session = new DbSession(_factory, _tenantContext.TenantId);
        var tenantId = session.TenantId > 0 ? (int?)session.TenantId : null;

        session.Connection.Execute(
            """
INSERT INTO dbo.SupplierPriceHistory (PartId, SupplierId, UnitPrice, CurrencyCode, Quantity, InvoiceId, RecordedAt, CreatedByUserId, TenantId)
VALUES (@PartId, @SupplierId, @UnitPrice, @CurrencyCode, @Quantity, @InvoiceId, SYSUTCDATETIME(), @UserId, @TenantId)
""",
            new
            {
                req.PartId,
                req.SupplierId,
                req.UnitPrice,
                CurrencyCode = req.CurrencyCode ?? "USD",
                req.Quantity,
                req.InvoiceId,
                UserId = userId,
                TenantId = tenantId
            },
            session.Transaction);
        session.Commit();
    }
}
