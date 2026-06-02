using Dapper;
using SpareParts.Domain.Forecasting;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Infrastructure.Services;

public sealed class ReorderAnalysisService
{
    private readonly ISqlConnectionFactory _factory;

    public ReorderAnalysisService(ISqlConnectionFactory factory)
    {
        _factory = factory;
    }

    public IReadOnlyList<ReorderRuleDto> GetRules()
    {
        using var session = new DbSession(_factory);
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
ORDER BY p.Name
""",
            transaction: session.Transaction).ToList();
    }

    public void UpsertRule(UpsertReorderRuleRequest req, int userId)
    {
        using var session = new DbSession(_factory);
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
        using var session = new DbSession(_factory);
        session.Connection.Execute(
            "DELETE FROM dbo.ReorderRules WHERE PartId = @PartId",
            new { PartId = partId },
            session.Transaction);
        session.Commit();
    }

    public IReadOnlyList<ReorderSuggestionDto> GetSuggestions()
    {
        using var session = new DbSession(_factory);
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
GROUP BY r.PartId, p.Name, p.InternalCode, r.ReorderPoint, r.ReorderQuantity,
         r.PreferredSupplierId, sup.Name
HAVING ISNULL(SUM(s.Quantity - ISNULL(s.ReservedQuantity, 0)), 0) <= r.ReorderPoint
ORDER BY CurrentStock ASC
""",
            transaction: session.Transaction).ToList();
    }
}
