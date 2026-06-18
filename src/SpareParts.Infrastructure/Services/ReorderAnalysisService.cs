using Dapper;
using SpareParts.Domain.Forecasting;
using SpareParts.Infrastructure.Data;
using SpareParts.Infrastructure.Interfaces;

namespace SpareParts.Infrastructure.Services;

public sealed class ReorderAnalysisService
{
    private readonly ISqlConnectionFactory _factory;
    private readonly ITenantContext _tenantContext;

    public ReorderAnalysisService(ISqlConnectionFactory factory, ITenantContext tenantContext)
    {
        _factory = factory;
        _tenantContext = tenantContext;
    }

    public IReadOnlyList<ReorderRuleDto> GetRules()
    {
        using var session = new DbSession(_factory, _tenantContext.TenantId);
        return session.Connection.Query<ReorderRuleDto>(
            """
SELECT
    r.Id,
    r.PartId,
    p.Name AS PartName,
    p.InternalCode AS PartCode,
    r.ReorderPoint,
    r.ReorderQuantity,
    r.PreferredSupplierId,
    s.Name AS PreferredSupplierName,
    r.IsActive,
    r.CreatedAt
FROM dbo.ReorderRules r
INNER JOIN dbo.Parts p ON p.Id = r.PartId
LEFT JOIN dbo.Suppliers s ON s.Id = r.PreferredSupplierId
WHERE (@TenantId = 0 OR p.TenantId = @TenantId)
ORDER BY p.Name
""",
            new { TenantId = _tenantContext.TenantId },
            transaction: session.Transaction).ToList();
    }

    public void UpsertRule(UpsertReorderRuleRequest req, int userId)
    {
        using var session = new DbSession(_factory, _tenantContext.TenantId);

        var partExists = session.Connection.ExecuteScalar<bool>(
            "SELECT CASE WHEN EXISTS (SELECT 1 FROM dbo.Parts WHERE Id = @PartId AND (@TenantId = 0 OR TenantId = @TenantId)) THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END;",
            new { req.PartId, TenantId = _tenantContext.TenantId },
            session.Transaction);

        if (!partExists)
            throw new NotFoundException($"Part {req.PartId} not found.");

        session.Connection.Execute(
            """
MERGE dbo.ReorderRules AS target
USING (SELECT @PartId AS PartId) AS source ON target.PartId = source.PartId
WHEN MATCHED THEN
    UPDATE SET
        ReorderPoint = @ReorderPoint,
        ReorderQuantity = @ReorderQuantity,
        PreferredSupplierId = @PreferredSupplierId,
        IsActive = @IsActive,
        ModifiedAt = SYSUTCDATETIME(),
        ModifiedByUserId = @UserId
WHEN NOT MATCHED THEN
    INSERT (PartId, ReorderPoint, ReorderQuantity, PreferredSupplierId, IsActive, CreatedAt, CreatedByUserId)
    VALUES (@PartId, @ReorderPoint, @ReorderQuantity, @PreferredSupplierId, @IsActive, SYSUTCDATETIME(), @UserId);
""",
            new
            {
                req.PartId,
                req.ReorderPoint,
                req.ReorderQuantity,
                req.PreferredSupplierId,
                req.IsActive,
                UserId = userId
            },
            session.Transaction);
        session.Commit();
    }

    public void DeleteRule(int partId)
    {
        using var session = new DbSession(_factory, _tenantContext.TenantId);
        session.Connection.Execute(
            """
DELETE r FROM dbo.ReorderRules r
INNER JOIN dbo.Parts p ON p.Id = r.PartId
WHERE r.PartId = @PartId AND (@TenantId = 0 OR p.TenantId = @TenantId)
""",
            new { PartId = partId, TenantId = _tenantContext.TenantId },
            session.Transaction);
        session.Commit();
    }

    public IReadOnlyList<ReorderSuggestionDto> GetSuggestions()
    {
        using var session = new DbSession(_factory, _tenantContext.TenantId);
        return session.Connection.Query<ReorderSuggestionDto>(
            """
SELECT
    r.PartId,
    p.Name AS PartName,
    p.InternalCode AS PartCode,
    ISNULL(SUM(s.Quantity - ISNULL(s.ReservedQuantity, 0)), 0) AS CurrentStock,
    r.ReorderPoint,
    r.ReorderQuantity AS SuggestedOrderQuantity,
    r.PreferredSupplierId,
    sup.Name AS PreferredSupplierName,
    (
        SELECT TOP 1 ti.UnitCost
        FROM dbo.TransactionItems ti
        INNER JOIN dbo.Transactions t ON t.Id = ti.TransactionId
        INNER JOIN dbo.TransactionTypes tt ON tt.Id = t.TransactionTypeId
        WHERE ti.PartId = r.PartId AND tt.TypeKey = 'Purchase'
        ORDER BY t.TransactionDate DESC
    ) AS LastPurchasePrice,
    ISNULL((
        SELECT SUM(ti.Quantity)
        FROM dbo.TransactionItems ti
        INNER JOIN dbo.Transactions t ON t.Id = ti.TransactionId
        INNER JOIN dbo.TransactionTypes tt ON tt.Id = t.TransactionTypeId
        WHERE ti.PartId = r.PartId AND tt.TypeKey = 'Sale'
          AND t.TransactionDate >= DATEADD(DAY, -30, SYSUTCDATETIME())
    ), 0) AS SalesLast30Days,
    ISNULL((
        SELECT SUM(ti.Quantity)
        FROM dbo.TransactionItems ti
        INNER JOIN dbo.Transactions t ON t.Id = ti.TransactionId
        INNER JOIN dbo.TransactionTypes tt ON tt.Id = t.TransactionTypeId
        WHERE ti.PartId = r.PartId AND tt.TypeKey = 'Sale'
          AND t.TransactionDate >= DATEADD(DAY, -90, SYSUTCDATETIME())
    ), 0) AS SalesLast90Days
FROM dbo.ReorderRules r
INNER JOIN dbo.Parts p ON p.Id = r.PartId
LEFT JOIN dbo.Stock s ON s.PartId = r.PartId
LEFT JOIN dbo.Suppliers sup ON sup.Id = r.PreferredSupplierId
WHERE r.IsActive = 1
  AND (@TenantId = 0 OR p.TenantId = @TenantId)
GROUP BY r.PartId, p.Name, p.InternalCode, r.ReorderPoint, r.ReorderQuantity,
         r.PreferredSupplierId, sup.Name
HAVING ISNULL(SUM(s.Quantity - ISNULL(s.ReservedQuantity, 0)), 0) <= r.ReorderPoint
ORDER BY CurrentStock ASC
""",
            new { TenantId = _tenantContext.TenantId },
            transaction: session.Transaction).ToList();
    }
}
