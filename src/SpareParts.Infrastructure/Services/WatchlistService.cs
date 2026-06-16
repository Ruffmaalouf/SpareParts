using Dapper;
using SpareParts.Domain.Watchlist;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Infrastructure.Services;

public sealed class WatchlistService
{
    private readonly ISqlConnectionFactory _factory;
    private readonly ITenantContext _tenantContext;

    public WatchlistService(ISqlConnectionFactory factory, ITenantContext tenantContext)
    {
        _factory = factory;
        _tenantContext = tenantContext;
    }

    public IEnumerable<WatchedPartDto> GetAllForUser(int userId)
    {
        using var session = new DbSession(_factory, _tenantContext.TenantId);
        return session.Connection.Query<WatchedPartDto>(
            """
SELECT
    Id,
    TenantId,
    UserId,
    PartName,
    VehicleMake,
    VehicleModel,
    VehicleYear,
    VinNumber,
    MaxBudget,
    Currency,
    Notes,
    IsActive,
    LastAlertedAt,
    CreatedAt
FROM dbo.WatchedParts
WHERE IsActive = 1
  AND UserId = @UserId
  AND (@TenantId = 0 OR TenantId = @TenantId)
ORDER BY CreatedAt DESC;
""",
            new { UserId = userId, TenantId = _tenantContext.TenantId },
            session.Transaction)
            .ToList();
    }

    public int Create(CreateWatchedPartRequest request, int userId, int? createdByUserId = null)
    {
        if (string.IsNullOrWhiteSpace(request.PartName)
            && string.IsNullOrWhiteSpace(request.VehicleMake)
            && string.IsNullOrWhiteSpace(request.VinNumber))
        {
            throw new ValidationException("At least a part name, vehicle make, or VIN must be specified.");
        }

        using var session = new DbSession(_factory, _tenantContext.TenantId);

        var id = session.Connection.ExecuteScalar<int>(
            """
INSERT INTO dbo.WatchedParts
(
    TenantId,
    UserId,
    PartName,
    VehicleMake,
    VehicleModel,
    VehicleYear,
    VinNumber,
    MaxBudget,
    Currency,
    Notes,
    IsActive,
    CreatedAt,
    CreatedByUserId
)
VALUES
(
    @TenantId,
    @UserId,
    @PartName,
    @VehicleMake,
    @VehicleModel,
    @VehicleYear,
    @VinNumber,
    @MaxBudget,
    @Currency,
    @Notes,
    1,
    @CreatedAt,
    @CreatedByUserId
);
SELECT CAST(SCOPE_IDENTITY() AS INT);
""",
            new
            {
                TenantId = _tenantContext.TenantId,
                UserId = userId,
                PartName = request.PartName,
                VehicleMake = request.VehicleMake,
                VehicleModel = request.VehicleModel,
                VehicleYear = request.VehicleYear,
                VinNumber = request.VinNumber,
                MaxBudget = request.MaxBudget,
                Currency = request.Currency ?? "USD",
                Notes = request.Notes,
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = createdByUserId
            },
            session.Transaction);

        session.Commit();
        return id;
    }

    public void Deactivate(int id, int userId)
    {
        using var session = new DbSession(_factory, _tenantContext.TenantId);

        var exists = session.Connection.ExecuteScalar<bool>(
            """
SELECT CASE WHEN EXISTS (
    SELECT 1 FROM dbo.WatchedParts
    WHERE Id = @Id AND UserId = @UserId AND IsActive = 1
    AND (@TenantId = 0 OR TenantId = @TenantId)
) THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END;
""",
            new { Id = id, UserId = userId, TenantId = _tenantContext.TenantId },
            session.Transaction);

        if (!exists)
            throw new NotFoundException("Watched part not found.");

        session.Connection.Execute(
            "UPDATE dbo.WatchedParts SET IsActive = 0 WHERE Id = @Id;",
            new { Id = id },
            session.Transaction);

        session.Commit();
    }

    public int GetMatchCount(int id)
    {
        using var session = new DbSession(_factory, _tenantContext.TenantId);

        var watchedPart = session.Connection.QuerySingleOrDefault<WatchedPartDto>(
            """
SELECT Id, TenantId, UserId, PartName, VehicleMake, VehicleModel, VehicleYear,
       VinNumber, MaxBudget, Currency, Notes, IsActive, LastAlertedAt, CreatedAt
FROM dbo.WatchedParts
WHERE Id = @Id AND IsActive = 1
  AND (@TenantId = 0 OR TenantId = @TenantId);
""",
            new { Id = id, TenantId = _tenantContext.TenantId },
            session.Transaction);

        if (watchedPart is null)
            throw new NotFoundException("Watched part not found.");

        var matchCount = session.Connection.ExecuteScalar<int>(
            """
SELECT COUNT(1)
FROM dbo.Parts p
WHERE p.IsActive = 1
  AND (@TenantId = 0 OR p.TenantId = @TenantId)
  AND (@PartName IS NULL OR p.Name LIKE '%' + @PartName + '%')
  AND (@MaxBudget IS NULL OR p.SalePrice <= @MaxBudget)
  AND EXISTS
  (
      SELECT 1
      FROM dbo.Stock s
      WHERE s.PartId = p.Id
        AND s.Quantity - ISNULL(s.ReservedQuantity, 0) > 0
  );
""",
            new
            {
                TenantId = _tenantContext.TenantId,
                PartName = string.IsNullOrWhiteSpace(watchedPart.PartName) ? null : watchedPart.PartName.Trim(),
                MaxBudget = watchedPart.MaxBudget
            },
            session.Transaction);

        if (matchCount > 0)
        {
            session.Connection.Execute(
                """
UPDATE dbo.WatchedParts
SET LastAlertedAt = @Now
WHERE Id = @Id;
""",
                new { Id = id, Now = DateTime.UtcNow },
                session.Transaction);

            session.Commit();
        }

        return matchCount;
    }
}
