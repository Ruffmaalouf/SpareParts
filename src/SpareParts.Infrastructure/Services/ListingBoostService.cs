using Dapper;
using SpareParts.Domain.Marketplace;
using SpareParts.Infrastructure.Data;
using SpareParts.Infrastructure.Interfaces;

namespace SpareParts.Infrastructure.Services;

public sealed class ListingBoostService
{
    private readonly ISqlConnectionFactory _factory;
    private readonly ITenantContext _tenantContext;

    public ListingBoostService(ISqlConnectionFactory factory, ITenantContext tenantContext)
    {
        _factory = factory;
        _tenantContext = tenantContext;
    }

    public IEnumerable<ListingBoostDto> GetAll(ListingBoostStatus? status = null)
    {
        using var session = new DbSession(_factory, _tenantContext.TenantId);
        return session.Connection.Query<ListingBoostDto>(
            """
SELECT
    lb.Id,
    lb.TenantId,
    lb.PartId,
    lb.DurationDays,
    lb.Fee,
    lb.Currency,
    lb.Status,
    lb.StartsAt,
    lb.ExpiresAt,
    lb.PaymentReference,
    lb.CreatedAt,
    p.Name AS PartName,
    p.InternalCode AS PartInternalCode,
    CASE
        WHEN lb.Status = 1 AND lb.ExpiresAt > SYSUTCDATETIME() THEN CAST(1 AS BIT)
        ELSE CAST(0 AS BIT)
    END AS IsCurrentlyActive
FROM dbo.ListingBoosts lb
LEFT JOIN dbo.Parts p ON p.Id = lb.PartId
WHERE lb.TenantId = @TenantId
  AND (@Status IS NULL OR lb.Status = @Status)
ORDER BY lb.CreatedAt DESC;
""",
            new
            {
                TenantId = _tenantContext.TenantId,
                Status = status.HasValue ? (int?)status.Value : null
            },
            session.Transaction)
            .ToList();
    }

    public int Create(CreateListingBoostRequest request, int createdByUserId)
    {
        if (request.PartId <= 0)
            throw new ValidationException("A valid part must be specified.");

        var now = DateTime.UtcNow;
        using var session = new DbSession(_factory, _tenantContext.TenantId);

        var id = session.Connection.ExecuteScalar<int>(
            """
INSERT INTO dbo.ListingBoosts
(
    TenantId,
    PartId,
    DurationDays,
    Fee,
    Currency,
    Status,
    StartsAt,
    ExpiresAt,
    PaymentReference,
    CreatedAt,
    CreatedByUserId
)
VALUES
(
    @TenantId,
    @PartId,
    @DurationDays,
    @Fee,
    @Currency,
    @Status,
    @StartsAt,
    @ExpiresAt,
    @PaymentReference,
    @CreatedAt,
    @CreatedByUserId
);
SELECT CAST(SCOPE_IDENTITY() AS INT);
""",
            new
            {
                TenantId = _tenantContext.TenantId,
                PartId = request.PartId,
                DurationDays = request.DurationDays,
                Fee = request.Fee,
                Currency = request.Currency ?? "USD",
                Status = (int)ListingBoostStatus.Active,
                StartsAt = now,
                ExpiresAt = now.AddDays(request.DurationDays),
                PaymentReference = request.PaymentReference,
                CreatedAt = now,
                CreatedByUserId = createdByUserId
            },
            session.Transaction);

        session.Commit();
        return id;
    }

    public void Cancel(int id, int modifiedByUserId)
    {
        using var session = new DbSession(_factory, _tenantContext.TenantId);

        var exists = session.Connection.ExecuteScalar<bool>(
            """
SELECT CASE WHEN EXISTS (
    SELECT 1 FROM dbo.ListingBoosts WHERE Id = @Id AND TenantId = @TenantId
) THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END;
""",
            new { Id = id, TenantId = _tenantContext.TenantId },
            session.Transaction);

        if (!exists)
            throw new NotFoundException("Listing boost not found.");

        session.Connection.Execute(
            """
UPDATE dbo.ListingBoosts
SET Status = @CancelledStatus,
    ModifiedAt = @Now,
    ModifiedByUserId = @ModifiedByUserId
WHERE Id = @Id;
""",
            new
            {
                Id = id,
                CancelledStatus = (int)ListingBoostStatus.Cancelled,
                Now = DateTime.UtcNow,
                ModifiedByUserId = modifiedByUserId
            },
            session.Transaction);

        session.Commit();
    }

    public bool IsPartBoosted(int partId)
    {
        using var session = new DbSession(_factory, _tenantContext.TenantId);
        return session.Connection.ExecuteScalar<bool>(
            """
SELECT CASE WHEN EXISTS (
    SELECT 1 FROM dbo.ListingBoosts
    WHERE PartId = @PartId
      AND TenantId = @TenantId
      AND Status = 1
      AND ExpiresAt > SYSUTCDATETIME()
) THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END;
""",
            new { PartId = partId, TenantId = _tenantContext.TenantId },
            session.Transaction);
    }
}
