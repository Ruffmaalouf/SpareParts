using Dapper;
using SpareParts.Domain.Warranty;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Infrastructure.Services;

public sealed class WarrantyService
{
    private readonly ISqlConnectionFactory _factory;

    public WarrantyService(ISqlConnectionFactory factory)
    {
        _factory = factory;
    }

    public IReadOnlyList<WarrantyClaimDto> GetClaims(string? status = null)
    {
        using var session = new DbSession(_factory);
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
ORDER BY wc.CreatedAt DESC
""",
            new { Status = status },
            session.Transaction).ToList();
    }

    public WarrantyClaimDto? GetClaim(int id)
    {
        using var session = new DbSession(_factory);
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
""",
            new { Id = id },
            session.Transaction);
    }

    public int CreateClaim(CreateWarrantyClaimRequest req, int userId)
    {
        using var session = new DbSession(_factory);

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
        using var session = new DbSession(_factory);
        var affected = session.Connection.Execute(
            """
UPDATE dbo.WarrantyClaims
SET Status = @Status,
    Resolution = @Resolution,
    RefundAmount = @RefundAmount,
    ResolvedAt = SYSUTCDATETIME(),
    ResolvedByUserId = @UserId
WHERE Id = @Id
""",
            new { Id = id, req.Status, req.Resolution, req.RefundAmount, UserId = userId },
            session.Transaction);

        if (affected == 0)
            throw new NotFoundException("Warranty claim not found.");

        session.Commit();
    }
}
