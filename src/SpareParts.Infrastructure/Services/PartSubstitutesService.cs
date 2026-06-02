using Dapper;
using SpareParts.Domain.Inventory;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Infrastructure.Services;

public sealed class PartSubstitutesService
{
    private readonly ISqlConnectionFactory _factory;

    public PartSubstitutesService(ISqlConnectionFactory factory)
    {
        _factory = factory;
    }

    public IReadOnlyList<PartSubstituteDto> GetSubstitutes(int partId)
    {
        using var session = new DbSession(_factory);
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
GROUP BY ps.Id, ps.PartId, ps.SubstitutePartId, sp.Name, sp.InternalCode, sp.SalePrice, ps.Notes
ORDER BY sp.Name
""",
            new { PartId = partId },
            session.Transaction).ToList();
    }

    public void AddSubstitute(int partId, AddPartSubstituteRequest req, int userId)
    {
        if (partId == req.SubstitutePartId)
            throw new ConflictException("A part cannot be a substitute for itself.");

        using var session = new DbSession(_factory);

        var exists = session.Connection.ExecuteScalar<int>(
            "SELECT COUNT(1) FROM dbo.PartSubstitutes WHERE PartId = @PartId AND SubstitutePartId = @SubId",
            new { PartId = partId, SubId = req.SubstitutePartId },
            session.Transaction);

        if (exists > 0)
            throw new ConflictException("This substitute relationship already exists.");

        session.Connection.Execute(
            """
INSERT INTO dbo.PartSubstitutes (PartId, SubstitutePartId, Notes, CreatedAt, CreatedByUserId)
VALUES (@PartId, @SubId, @Notes, SYSUTCDATETIME(), @UserId);

IF NOT EXISTS (SELECT 1 FROM dbo.PartSubstitutes WHERE PartId = @SubId AND SubstitutePartId = @PartId)
BEGIN
    INSERT INTO dbo.PartSubstitutes (PartId, SubstitutePartId, Notes, CreatedAt, CreatedByUserId)
    VALUES (@SubId, @PartId, @Notes, SYSUTCDATETIME(), @UserId);
END;
""",
            new { PartId = partId, SubId = req.SubstitutePartId, req.Notes, UserId = userId },
            session.Transaction);

        session.Commit();
    }

    public void RemoveSubstitute(int substituteId)
    {
        using var session = new DbSession(_factory);

        var row = session.Connection.QueryFirstOrDefault<(int PartId, int SubstitutePartId)>(
            "SELECT PartId, SubstitutePartId FROM dbo.PartSubstitutes WHERE Id = @Id",
            new { Id = substituteId },
            session.Transaction);

        if (row == default)
            throw new NotFoundException("Substitute relationship not found.");

        session.Connection.Execute(
            """
DELETE FROM dbo.PartSubstitutes
WHERE (PartId = @PartId AND SubstitutePartId = @SubId)
   OR (PartId = @SubId AND SubstitutePartId = @PartId);
""",
            new { row.PartId, SubId = row.SubstitutePartId },
            session.Transaction);

        session.Commit();
    }
}
