using System.ComponentModel.DataAnnotations;
using Dapper;
using SpareParts.Domain.Inspection;
using SpareParts.Infrastructure.Data;
using SpareParts.Infrastructure.Interfaces;

namespace SpareParts.Infrastructure.Services;

public sealed class LiveInspectionService
{
    private readonly ISqlConnectionFactory _factory;
    private readonly ITenantContext _tenantContext;

    public LiveInspectionService(ISqlConnectionFactory factory, ITenantContext tenantContext)
    {
        _factory = factory;
        _tenantContext = tenantContext;
    }

    public IEnumerable<InspectionRequestDto> GetAll(InspectionStatus? status = null)
    {
        using var session = new DbSession(_factory, _tenantContext.TenantId);

        return session.Connection.Query<InspectionRequestDto>(
            """
SELECT
    ir.Id, ir.TenantId, ir.PartId,
    p.Name AS PartName,
    ir.BuyerName, ir.BuyerPhone, ir.RequestedAt, ir.Notes, ir.Status,
    CASE ir.Status
        WHEN 1 THEN 'Pending' WHEN 2 THEN 'Accepted' WHEN 3 THEN 'Rejected'
        WHEN 4 THEN 'Completed' WHEN 5 THEN 'Cancelled' ELSE 'Unknown'
    END AS StatusLabel,
    ir.VideoProvider, ir.VideoRoomUrl, ir.CreatedAt
FROM dbo.InspectionRequests ir
LEFT JOIN dbo.Parts p ON p.Id = ir.PartId
WHERE (@TenantId = 0 OR ir.TenantId = @TenantId)
  AND (@Status IS NULL OR ir.Status = @Status)
ORDER BY ir.CreatedAt DESC;
""",
            new { TenantId = _tenantContext.TenantId, Status = (int?)status },
            session.Transaction);
    }

    public InspectionRequestDto? GetById(int id)
    {
        using var session = new DbSession(_factory, _tenantContext.TenantId);

        return session.Connection.QuerySingleOrDefault<InspectionRequestDto>(
            """
SELECT
    ir.Id, ir.TenantId, ir.PartId,
    p.Name AS PartName,
    ir.BuyerName, ir.BuyerPhone, ir.RequestedAt, ir.Notes, ir.Status,
    CASE ir.Status
        WHEN 1 THEN 'Pending' WHEN 2 THEN 'Accepted' WHEN 3 THEN 'Rejected'
        WHEN 4 THEN 'Completed' WHEN 5 THEN 'Cancelled' ELSE 'Unknown'
    END AS StatusLabel,
    ir.VideoProvider, ir.VideoRoomUrl, ir.CreatedAt
FROM dbo.InspectionRequests ir
LEFT JOIN dbo.Parts p ON p.Id = ir.PartId
WHERE ir.Id = @Id AND (@TenantId = 0 OR ir.TenantId = @TenantId);
""",
            new { Id = id, TenantId = _tenantContext.TenantId },
            session.Transaction);
    }

    public InspectionRequestDto Create(CreateInspectionRequest request, int createdByUserId)
    {
        if (request.PartId <= 0)
            throw new ValidationException("A valid part must be specified.");
        if (string.IsNullOrWhiteSpace(request.BuyerName))
            throw new ValidationException("Buyer name is required.");

        var now = DateTime.UtcNow;

        using var session = new DbSession(_factory, _tenantContext.TenantId);

        var id = session.Connection.ExecuteScalar<int>(
            """
INSERT INTO dbo.InspectionRequests (TenantId, PartId, BuyerName, BuyerPhone, RequestedAt, Notes, Status, CreatedAt, CreatedByUserId)
VALUES (@TenantId, @PartId, @BuyerName, @BuyerPhone, @RequestedAt, @Notes, 1, @CreatedAt, @CreatedByUserId);
SELECT SCOPE_IDENTITY();
""",
            new
            {
                TenantId = _tenantContext.TenantId,
                PartId = request.PartId,
                BuyerName = request.BuyerName,
                BuyerPhone = request.BuyerPhone,
                RequestedAt = request.RequestedAt,
                Notes = request.Notes,
                CreatedAt = now,
                CreatedByUserId = createdByUserId
            },
            session.Transaction);

        session.Commit();

        return new InspectionRequestDto
        {
            Id = id,
            TenantId = _tenantContext.TenantId,
            PartId = request.PartId,
            BuyerName = request.BuyerName,
            BuyerPhone = request.BuyerPhone,
            RequestedAt = request.RequestedAt,
            Notes = request.Notes,
            Status = InspectionStatus.Pending,
            StatusLabel = "Pending",
            CreatedAt = now
        };
    }

    public void UpdateStatus(int id, UpdateInspectionStatusRequest request)
    {
        using var session = new DbSession(_factory, _tenantContext.TenantId);

        session.Connection.Execute(
            """
UPDATE dbo.InspectionRequests
SET Status = @Status,
    Notes = COALESCE(@Notes, Notes),
    VideoRoomUrl = COALESCE(@VideoRoomUrl, VideoRoomUrl),
    ModifiedAt = @Now
WHERE Id = @Id AND (@TenantId = 0 OR TenantId = @TenantId);
""",
            new
            {
                Id = id,
                Status = (int)request.NewStatus,
                Notes = request.Notes,
                VideoRoomUrl = request.VideoRoomUrl,
                Now = DateTime.UtcNow,
                TenantId = _tenantContext.TenantId
            },
            session.Transaction);

        session.Commit();
    }
}
