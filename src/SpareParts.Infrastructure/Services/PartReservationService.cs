using Dapper;
using SpareParts.Domain.Marketplace;
using SpareParts.Infrastructure.Data;
using SpareParts.Infrastructure.Interfaces;

namespace SpareParts.Infrastructure.Services;

public sealed class PartReservationService
{
    private readonly ISqlConnectionFactory _factory;
    private readonly ITenantContext _tenantContext;

    public PartReservationService(ISqlConnectionFactory factory, ITenantContext tenantContext)
    {
        _factory = factory;
        _tenantContext = tenantContext;
    }

    public IEnumerable<PartReservationDto> GetAllForTenant(PartReservationStatus? status = null)
    {
        using var session = new DbSession(_factory, _tenantContext.TenantId);
        var items = session.Connection.Query<PartReservationDto>(
            """
SELECT pr.Id, pr.TenantId, pr.PartId, pr.ReservedByUserId, pr.ReservedByName, pr.ReservedByPhone,
       pr.Quantity, pr.HoldFee, pr.Currency, pr.Status,
       CASE pr.Status WHEN 1 THEN 'Active' WHEN 2 THEN 'Released' WHEN 3 THEN 'Expired' WHEN 4 THEN 'Fulfilled' ELSE 'Unknown' END AS StatusLabel,
       pr.ExpiresAt, pr.Notes, pr.CreatedAt,
       p.Name AS PartName, p.InternalCode AS PartInternalCode
FROM dbo.PartReservations pr
LEFT JOIN dbo.Parts p ON p.Id = pr.PartId
WHERE (@TenantId = 0 OR pr.TenantId = @TenantId)
  AND (@Status IS NULL OR pr.Status = @Status)
ORDER BY pr.CreatedAt DESC;
""",
            new { TenantId = _tenantContext.TenantId, Status = (int?)status },
            transaction: session.Transaction).ToList();
        session.Commit();
        return items;
    }

    public PartReservationDto? GetById(int id)
    {
        using var session = new DbSession(_factory, _tenantContext.TenantId);
        var item = session.Connection.QueryFirstOrDefault<PartReservationDto>(
            """
SELECT pr.Id, pr.TenantId, pr.PartId, pr.ReservedByUserId, pr.ReservedByName, pr.ReservedByPhone,
       pr.Quantity, pr.HoldFee, pr.Currency, pr.Status,
       CASE pr.Status WHEN 1 THEN 'Active' WHEN 2 THEN 'Released' WHEN 3 THEN 'Expired' WHEN 4 THEN 'Fulfilled' ELSE 'Unknown' END AS StatusLabel,
       pr.ExpiresAt, pr.Notes, pr.CreatedAt,
       p.Name AS PartName, p.InternalCode AS PartInternalCode
FROM dbo.PartReservations pr
LEFT JOIN dbo.Parts p ON p.Id = pr.PartId
WHERE pr.Id = @Id AND (@TenantId = 0 OR pr.TenantId = @TenantId);
""",
            new { Id = id, TenantId = _tenantContext.TenantId },
            transaction: session.Transaction);
        session.Commit();
        return item;
    }

    public int Create(CreatePartReservationRequest request, int createdByUserId)
    {
        using var session = new DbSession(_factory, _tenantContext.TenantId);
        var expiresAt = DateTime.UtcNow.AddHours(request.HoldHours);
        var id = session.Connection.QuerySingle<int>(
            """
INSERT INTO dbo.PartReservations
    (TenantId, PartId, ReservedByUserId, ReservedByName, ReservedByPhone, Quantity,
     HoldFee, Currency, Status, ExpiresAt, Notes, CreatedAt, CreatedByUserId)
VALUES
    (@TenantId, @PartId, @ReservedByUserId, @ReservedByName, @ReservedByPhone, @Quantity,
     NULL, 'USD', @Status, @ExpiresAt, @Notes, SYSUTCDATETIME(), @CreatedByUserId);
SELECT SCOPE_IDENTITY();
""",
            new
            {
                TenantId = _tenantContext.TenantId,
                request.PartId,
                ReservedByUserId = (int?)null,
                request.ReservedByName,
                request.ReservedByPhone,
                request.Quantity,
                Status = (int)PartReservationStatus.Active,
                ExpiresAt = expiresAt,
                request.Notes,
                CreatedByUserId = createdByUserId
            },
            transaction: session.Transaction);
        session.Commit();
        return id;
    }

    public void Release(int id, int modifiedByUserId)
    {
        using var session = new DbSession(_factory, _tenantContext.TenantId);
        session.Connection.Execute(
            """
UPDATE dbo.PartReservations
SET Status = @Status, ModifiedAt = SYSUTCDATETIME(), ModifiedByUserId = @ModifiedByUserId
WHERE Id = @Id AND (@TenantId = 0 OR TenantId = @TenantId);
""",
            new { Id = id, Status = (int)PartReservationStatus.Released, TenantId = _tenantContext.TenantId, ModifiedByUserId = modifiedByUserId },
            transaction: session.Transaction);
        session.Commit();
    }

    public void ExpireOverdue()
    {
        using var conn = _factory.CreateConnection();
        conn.Execute(
            "UPDATE dbo.PartReservations SET Status = @Expired, ModifiedAt = SYSUTCDATETIME() WHERE Status = @Active AND ExpiresAt <= SYSUTCDATETIME();",
            new { Expired = (int)PartReservationStatus.Expired, Active = (int)PartReservationStatus.Active });
    }
}
