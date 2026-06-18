using Dapper;
using SpareParts.Domain.Warranty;
using SpareParts.Infrastructure.Data;
using SpareParts.Infrastructure.Interfaces;

namespace SpareParts.Infrastructure.Services;

public sealed class WarrantyService
{
    private readonly ISqlConnectionFactory _factory;
    private readonly ITenantContext _tenantContext;

    public WarrantyService(ISqlConnectionFactory factory, ITenantContext tenantContext)
    {
        _factory = factory;
        _tenantContext = tenantContext;
    }

    public IReadOnlyList<WarrantyClaimDto> GetClaims(string? status = null)
    {
        using var session = new DbSession(_factory, _tenantContext.TenantId);
        return session.Connection.Query<WarrantyClaimDto>(
            """
SELECT TOP 500
    wc.Id,
    wc.ClaimNumber,
    wc.CustomerId,
    c.Name AS CustomerName,
    wc.PartId,
    p.Name AS PartName,
    p.InternalCode AS PartCode,
    wc.Quantity,
    wc.ClaimType,
    wc.Status,
    wc.Description,
    wc.Resolution,
    wc.OriginalInvoiceId,
    wc.RefundAmount,
    wc.CreatedAt,
    wc.ResolvedAt
FROM dbo.WarrantyClaims wc
INNER JOIN dbo.Parts p ON p.Id = wc.PartId
LEFT JOIN dbo.Customers c ON c.Id = wc.CustomerId
WHERE (@Status IS NULL OR wc.Status = @Status)
  AND (@TenantId = 0 OR p.TenantId = @TenantId)
ORDER BY wc.CreatedAt DESC
""",
            new { Status = status, TenantId = _tenantContext.TenantId },
            session.Transaction).ToList();
    }

    public WarrantyClaimDto? GetClaim(int id)
    {
        using var session = new DbSession(_factory, _tenantContext.TenantId);
        return session.Connection.QueryFirstOrDefault<WarrantyClaimDto>(
            """
SELECT
    wc.Id, wc.ClaimNumber, wc.CustomerId, c.Name AS CustomerName,
    wc.PartId, p.Name AS PartName, p.InternalCode AS PartCode,
    wc.Quantity, wc.ClaimType, wc.Status, wc.Description,
    wc.Resolution, wc.OriginalInvoiceId, wc.RefundAmount,
    wc.CreatedAt, wc.ResolvedAt
FROM dbo.WarrantyClaims wc
INNER JOIN dbo.Parts p ON p.Id = wc.PartId
LEFT JOIN dbo.Customers c ON c.Id = wc.CustomerId
WHERE wc.Id = @Id
  AND (@TenantId = 0 OR p.TenantId = @TenantId)
""",
            new { Id = id, TenantId = _tenantContext.TenantId },
            session.Transaction);
    }

    public int CreateClaim(CreateWarrantyClaimRequest req, int userId)
    {
        using var session = new DbSession(_factory, _tenantContext.TenantId);

        var partTenantId = session.Connection.ExecuteScalar<int?>(
            "SELECT TenantId FROM dbo.Parts WHERE Id = @PartId;",
            new { req.PartId },
            session.Transaction);

        if (partTenantId is null || (_tenantContext.TenantId != 0 && partTenantId != _tenantContext.TenantId))
            throw new NotFoundException("Part not found.");

        var id = session.Connection.ExecuteScalar<int>(
            """
INSERT INTO dbo.WarrantyClaims
    (CustomerId, PartId, Quantity, ClaimType, Status, Description, OriginalInvoiceId, CreatedAt, CreatedByUserId)
VALUES
    (@CustomerId, @PartId, @Quantity, @ClaimType, 'Open', @Description, @OriginalInvoiceId, SYSUTCDATETIME(), @UserId);

DECLARE @NewId INT = CAST(SCOPE_IDENTITY() AS INT);
UPDATE dbo.WarrantyClaims SET ClaimNumber = 'WC-' + CAST(@NewId AS NVARCHAR(20)) WHERE Id = @NewId;
SELECT @NewId;
""",
            new
            {
                req.CustomerId,
                req.PartId,
                req.Quantity,
                req.ClaimType,
                req.Description,
                req.OriginalInvoiceId,
                UserId = userId
            },
            session.Transaction);

        session.Commit();
        return id;
    }

    public void ResolveClaim(int id, ResolveWarrantyClaimRequest req, int userId)
    {
        using var session = new DbSession(_factory, _tenantContext.TenantId);
        var affected = session.Connection.Execute(
            """
UPDATE dbo.WarrantyClaims
SET Status = @Status,
    Resolution = @Resolution,
    RefundAmount = @RefundAmount,
    ResolvedAt = SYSUTCDATETIME(),
    ResolvedByUserId = @UserId
WHERE Id = @Id
  AND EXISTS (
      SELECT 1 FROM dbo.Parts p
      WHERE p.Id = dbo.WarrantyClaims.PartId
        AND (@TenantId = 0 OR p.TenantId = @TenantId)
  )
""",
            new { Id = id, req.Status, req.Resolution, req.RefundAmount, UserId = userId, TenantId = _tenantContext.TenantId },
            session.Transaction);

        if (affected == 0)
            throw new NotFoundException("Warranty claim not found.");

        session.Commit();
    }
}
