using Dapper;
using SpareParts.Domain.Inventory;
using SpareParts.Infrastructure.Data;
using SpareParts.Infrastructure.Interfaces;

namespace SpareParts.Infrastructure.Services;

public sealed class PartSubstitutesService
{
    private readonly ISqlConnectionFactory _factory;
    private readonly ITenantContext _tenantContext;

    public PartSubstitutesService(ISqlConnectionFactory factory, ITenantContext tenantContext)
    {
        _factory = factory;
        _tenantContext = tenantContext;
    }

    public IReadOnlyList<PartSubstituteDto> GetSubstitutes(int partId)
    {
        using var session = new DbSession(_factory, _tenantContext.TenantId);
        return session.Connection.Query<PartSubstituteDto>(
            """
SELECT
    ps.Id,
    ps.PartId,
    ps.SubstitutePartId,
    sp.Name AS SubstitutePartName,
    sp.InternalCode AS SubstitutePartCode,
    ISNULL(SUM(s.Quantity - ISNULL(s.ReservedQuantity, 0)), 0) AS SubstituteAvailableStock,
    sp.SalePrice AS SubstituteSalePrice,
    ps.Notes
FROM dbo.PartSubstitutes ps
INNER JOIN dbo.Parts sp ON sp.Id = ps.SubstitutePartId
LEFT JOIN dbo.Stock s ON s.PartId = ps.SubstitutePartId
WHERE ps.PartId = @PartId AND sp.IsActive = 1
  AND (@TenantId = 0 OR ps.TenantId = @TenantId)
  AND (@TenantId = 0 OR sp.TenantId = @TenantId)
GROUP BY ps.Id, ps.PartId, ps.SubstitutePartId, sp.Name, sp.InternalCode, sp.SalePrice, ps.Notes
ORDER BY sp.Name
""",
            new { PartId = partId, session.TenantId },
            session.Transaction).ToList();
    }

    public void AddSubstitute(int partId, AddPartSubstituteRequest req, int userId)
    {
        if (partId == req.SubstitutePartId)
            throw new ConflictException("A part cannot be a substitute for itself.");

        using var session = new DbSession(_factory, _tenantContext.TenantId);

        var exists = session.Connection.ExecuteScalar<int>(
            """
SELECT COUNT(1) FROM dbo.PartSubstitutes
WHERE PartId = @PartId AND SubstitutePartId = @SubId
  AND (@TenantId = 0 OR TenantId = @TenantId)
""",
            new { PartId = partId, SubId = req.SubstitutePartId, session.TenantId },
            session.Transaction);

        if (exists > 0)
            throw new ConflictException("This substitute relationship already exists.");

        var tenantId = session.TenantId > 0 ? (int?)session.TenantId : null;

        session.Connection.Execute(
            """
INSERT INTO dbo.PartSubstitutes (PartId, SubstitutePartId, Notes, CreatedAt, CreatedByUserId, TenantId)
VALUES (@PartId, @SubId, @Notes, SYSUTCDATETIME(), @UserId, @TenantId);

IF NOT EXISTS (SELECT 1 FROM dbo.PartSubstitutes WHERE PartId = @SubId AND SubstitutePartId = @PartId)
BEGIN
    INSERT INTO dbo.PartSubstitutes (PartId, SubstitutePartId, Notes, CreatedAt, CreatedByUserId, TenantId)
    VALUES (@SubId, @PartId, @Notes, SYSUTCDATETIME(), @UserId, @TenantId);
END;
""",
            new { PartId = partId, SubId = req.SubstitutePartId, req.Notes, UserId = userId, TenantId = tenantId },
            session.Transaction);

        session.Commit();
    }

    public void RemoveSubstitute(int substituteId)
    {
        using var session = new DbSession(_factory, _tenantContext.TenantId);

        var row = session.Connection.QueryFirstOrDefault<(int PartId, int SubstitutePartId)>(
            """
SELECT PartId, SubstitutePartId FROM dbo.PartSubstitutes
WHERE Id = @Id AND (@TenantId = 0 OR TenantId = @TenantId)
""",
            new { Id = substituteId, session.TenantId },
            session.Transaction);

        if (row == default)
            throw new NotFoundException("Substitute relationship not found.");

        session.Connection.Execute(
            """
DELETE FROM dbo.PartSubstitutes
WHERE ((PartId = @PartId AND SubstitutePartId = @SubId)
   OR (PartId = @SubId AND SubstitutePartId = @PartId))
  AND (@TenantId = 0 OR TenantId = @TenantId);
""",
            new { row.PartId, SubId = row.SubstitutePartId, session.TenantId },
            session.Transaction);

        session.Commit();
    }
}
