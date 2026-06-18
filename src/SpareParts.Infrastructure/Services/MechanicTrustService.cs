using System.ComponentModel.DataAnnotations;
using Dapper;
using SpareParts.Domain.Mechanic;
using SpareParts.Infrastructure.Data;
using SpareParts.Infrastructure.Interfaces;

namespace SpareParts.Infrastructure.Services;

public sealed class MechanicTrustService
{
    private readonly ISqlConnectionFactory _factory;
    private readonly ITenantContext _tenantContext;

    public MechanicTrustService(ISqlConnectionFactory factory, ITenantContext tenantContext)
    {
        _factory = factory;
        _tenantContext = tenantContext;
    }

    public IEnumerable<MechanicProfileDto> GetAll()
    {
        using var session = new DbSession(_factory, _tenantContext.TenantId);

        return session.Connection.Query<MechanicProfileDto>(
            """
SELECT
    mp.Id, mp.TenantId, mp.UserId,
    u.FullName AS UserName,
    mp.BadgeLevel,
    CASE mp.BadgeLevel WHEN 0 THEN 'None' WHEN 1 THEN 'Emerging' WHEN 2 THEN 'Verified' WHEN 3 THEN 'Elite' ELSE 'None' END AS BadgeLevelLabel,
    mp.SpecialtyBrands, mp.SpecialtyTypes,
    mp.PurchaseCount, mp.RepairOrderCount,
    mp.IsVerified, mp.VerifiedAt, mp.CreatedAt
FROM dbo.MechanicProfiles mp
LEFT JOIN dbo.Users u ON u.Id = mp.UserId
WHERE (@TenantId = 0 OR mp.TenantId = @TenantId)
ORDER BY mp.BadgeLevel DESC, mp.PurchaseCount DESC;
""",
            new { TenantId = _tenantContext.TenantId },
            session.Transaction);
    }

    public MechanicProfileDto? GetByUserId(int userId)
    {
        using var session = new DbSession(_factory, _tenantContext.TenantId);

        return session.Connection.QuerySingleOrDefault<MechanicProfileDto>(
            """
SELECT
    mp.Id, mp.TenantId, mp.UserId,
    u.FullName AS UserName,
    mp.BadgeLevel,
    CASE mp.BadgeLevel WHEN 0 THEN 'None' WHEN 1 THEN 'Emerging' WHEN 2 THEN 'Verified' WHEN 3 THEN 'Elite' ELSE 'None' END AS BadgeLevelLabel,
    mp.SpecialtyBrands, mp.SpecialtyTypes,
    mp.PurchaseCount, mp.RepairOrderCount,
    mp.IsVerified, mp.VerifiedAt, mp.CreatedAt
FROM dbo.MechanicProfiles mp
LEFT JOIN dbo.Users u ON u.Id = mp.UserId
WHERE mp.UserId = @UserId AND (@TenantId = 0 OR mp.TenantId = @TenantId);
""",
            new { UserId = userId, TenantId = _tenantContext.TenantId },
            session.Transaction);
    }

    public MechanicProfileDto CreateOrUpdate(CreateMechanicProfileRequest request, int createdByUserId)
    {
        if (request.UserId <= 0)
            throw new ValidationException("A valid user must be specified.");

        using var session = new DbSession(_factory, _tenantContext.TenantId);

        var existing = session.Connection.ExecuteScalar<int?>(
            "SELECT Id FROM dbo.MechanicProfiles WHERE UserId = @UserId AND (@TenantId = 0 OR TenantId = @TenantId);",
            new { UserId = request.UserId, TenantId = _tenantContext.TenantId },
            session.Transaction);

        var now = DateTime.UtcNow;

        if (existing.HasValue)
        {
            session.Connection.Execute(
                """
UPDATE dbo.MechanicProfiles
SET SpecialtyBrands = @SpecialtyBrands, SpecialtyTypes = @SpecialtyTypes
WHERE Id = @Id;
""",
                new { Id = existing.Value, SpecialtyBrands = request.SpecialtyBrands, SpecialtyTypes = request.SpecialtyTypes },
                session.Transaction);
        }
        else
        {
            session.Connection.Execute(
                """
INSERT INTO dbo.MechanicProfiles (TenantId, UserId, BadgeLevel, SpecialtyBrands, SpecialtyTypes, PurchaseCount, RepairOrderCount, IsVerified, CreatedAt, CreatedByUserId)
VALUES (@TenantId, @UserId, 0, @SpecialtyBrands, @SpecialtyTypes, 0, 0, 0, @CreatedAt, @CreatedByUserId);
""",
                new
                {
                    TenantId = _tenantContext.TenantId,
                    UserId = request.UserId,
                    SpecialtyBrands = request.SpecialtyBrands,
                    SpecialtyTypes = request.SpecialtyTypes,
                    CreatedAt = now,
                    CreatedByUserId = createdByUserId
                },
                session.Transaction);
        }

        session.Commit();

        return GetByUserId(request.UserId)!;
    }

    public void Verify(int userId)
    {
        using var session = new DbSession(_factory, _tenantContext.TenantId);

        session.Connection.Execute(
            """
UPDATE dbo.MechanicProfiles
SET IsVerified = 1, VerifiedAt = @Now,
    BadgeLevel = CASE WHEN BadgeLevel < 2 THEN 2 ELSE BadgeLevel END
WHERE UserId = @UserId AND (@TenantId = 0 OR TenantId = @TenantId);
""",
            new { UserId = userId, Now = DateTime.UtcNow, TenantId = _tenantContext.TenantId },
            session.Transaction);

        session.Commit();
    }
}
