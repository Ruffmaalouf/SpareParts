using Dapper;
using SpareParts.Domain.Purchases;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Infrastructure.Services;

public sealed class SupplierPriceHistoryService
{
    private readonly ISqlConnectionFactory _factory;

    public SupplierPriceHistoryService(ISqlConnectionFactory factory)
    {
        _factory = factory;
    }

    public IReadOnlyList<SupplierPriceHistoryDto> GetHistory(int partId)
    {
        using var session = new DbSession(_factory);
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
ORDER BY sph.RecordedAt DESC
""",
            new { PartId = partId },
            session.Transaction).ToList();
    }

    public SupplierPriceComparisonDto? GetComparison(int partId)
    {
        using var session = new DbSession(_factory);

        var part = session.Connection.QueryFirstOrDefault<(string Name, string Code)>(
            "SELECT Name, InternalCode AS Code FROM dbo.Parts WHERE Id = @PartId AND IsActive = 1",
            new { PartId = partId },
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
GROUP BY s.Id, s.Name
ORDER BY LastPrice ASC
""",
            new { PartId = partId },
            session.Transaction).ToList();

        // Correct LastPrice to be the most recent (not MAX)
        foreach (var sp in supplierPrices)
        {
            var lastPrice = session.Connection.ExecuteScalar<decimal>(
                """
SELECT TOP 1 UnitPrice
FROM dbo.SupplierPriceHistory
WHERE PartId = @PartId AND SupplierId = @SupplierId
ORDER BY RecordedAt DESC
""",
                new { PartId = partId, SupplierId = sp.SupplierId },
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
        using var session = new DbSession(_factory);
        session.Connection.Execute(
            """
INSERT INTO dbo.SupplierPriceHistory (PartId, SupplierId, UnitPrice, CurrencyCode, Quantity, InvoiceId, RecordedAt, CreatedByUserId)
VALUES (@PartId, @SupplierId, @UnitPrice, @CurrencyCode, @Quantity, @InvoiceId, SYSUTCDATETIME(), @UserId)
""",
            new
            {
                req.PartId,
                req.SupplierId,
                req.UnitPrice,
                CurrencyCode = req.CurrencyCode ?? "USD",
                req.Quantity,
                req.InvoiceId,
                UserId = userId
            },
            session.Transaction);
        session.Commit();
    }
}
