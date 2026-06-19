using Dapper;
using SpareParts.Domain.Inventory;
using SpareParts.Infrastructure.Data;
using SpareParts.Infrastructure.Interfaces;

namespace SpareParts.Infrastructure.Services;

public sealed class PartExpiryService
{
    private readonly ISqlConnectionFactory _factory;
    private readonly ITenantContext _tenantContext;

    public PartExpiryService(ISqlConnectionFactory factory, ITenantContext tenantContext)
    {
        _factory = factory;
        _tenantContext = tenantContext;
    }

    public IReadOnlyList<PartExpiryAlertDto> GetExpiryAlerts(int daysAhead = 90)
    {
        using var session = new DbSession(_factory, _tenantContext.TenantId);
        return session.Connection.Query<PartExpiryAlertDto>(
            """
SELECT
    p.Id AS PartId,
    p.Name AS PartName,
    p.InternalCode AS PartCode,
    p.ExpiryDate,
    DATEDIFF(DAY, SYSUTCDATETIME(), p.ExpiryDate) AS DaysUntilExpiry,
    ISNULL(SUM(s.Quantity - ISNULL(s.ReservedQuantity, 0)), 0) AS StockQuantity,
    CASE
        WHEN p.ExpiryDate < SYSUTCDATETIME() THEN 'Expired'
        WHEN p.ExpiryDate < DATEADD(DAY, 30, SYSUTCDATETIME()) THEN 'Critical'
        ELSE 'Warning'
    END AS ExpiryStatus
FROM dbo.Parts p
LEFT JOIN dbo.Stock s ON s.PartId = p.Id AND (@TenantId = 0 OR s.TenantId = @TenantId)
WHERE p.IsActive = 1
  AND p.ExpiryDate IS NOT NULL
  AND p.ExpiryDate <= DATEADD(DAY, @DaysAhead, SYSUTCDATETIME())
  AND (@TenantId = 0 OR p.TenantId = @TenantId)
GROUP BY p.Id, p.Name, p.InternalCode, p.ExpiryDate
HAVING ISNULL(SUM(s.Quantity - ISNULL(s.ReservedQuantity, 0)), 0) > 0
ORDER BY p.ExpiryDate ASC
""",
            new { DaysAhead = daysAhead, session.TenantId },
            session.Transaction).ToList();
    }

    public void SetExpiry(int partId, SetPartExpiryRequest req, int userId)
    {
        using var session = new DbSession(_factory, _tenantContext.TenantId);
        var affected = session.Connection.Execute(
            """
UPDATE dbo.Parts
SET ExpiryDate = @ExpiryDate,
    ShelfLifeDays = @ShelfLifeDays,
    ModifiedAt = SYSUTCDATETIME(),
    ModifiedByUserId = @UserId
WHERE Id = @PartId AND IsActive = 1 AND (@TenantId = 0 OR TenantId = @TenantId)
""",
            new { PartId = partId, req.ExpiryDate, req.ShelfLifeDays, UserId = userId, session.TenantId },
            session.Transaction);

        if (affected == 0)
            throw new NotFoundException("Part not found.");

        session.Commit();
    }
}
