using System.ComponentModel.DataAnnotations;
using Dapper;
using SpareParts.Domain.Trust;
using SpareParts.Infrastructure.Data;
using SpareParts.Infrastructure.Interfaces;

namespace SpareParts.Infrastructure.Services;

public sealed class CommunityGuardService
{
    private readonly ISqlConnectionFactory _factory;
    private readonly ITenantContext _tenantContext;
    private const int AutoFlagThreshold = 3;

    public CommunityGuardService(ISqlConnectionFactory factory, ITenantContext tenantContext)
    {
        _factory = factory;
        _tenantContext = tenantContext;
    }

    public IEnumerable<ContentReportDto> GetReports(ReportStatus? status = null, ReportTargetType? targetType = null)
    {
        using var session = new DbSession(_factory, _tenantContext.TenantId);

        return session.Connection.Query<ContentReportDto>(
            """
SELECT
    Id, TenantId, TargetType, TargetId, ReportedByUserId, Category,
    CASE Category
        WHEN 1 THEN 'WrongPartSent' WHEN 2 THEN 'FakePhotos'
        WHEN 3 THEN 'WorseConditionThanDescribed' WHEN 4 THEN 'SellerDisappeared'
        WHEN 5 THEN 'DuplicateListing' WHEN 6 THEN 'ScamSuspicion' ELSE 'Unknown'
    END AS CategoryLabel,
    Details,
    Status,
    CASE Status
        WHEN 1 THEN 'Pending' WHEN 2 THEN 'UnderReview'
        WHEN 3 THEN 'Dismissed' WHEN 4 THEN 'ActionTaken' ELSE 'Unknown'
    END AS StatusLabel,
    AutoFlagged, CreatedAt
FROM dbo.ContentReports
WHERE TenantId = @TenantId
  AND (@Status IS NULL OR Status = @Status)
  AND (@TargetType IS NULL OR TargetType = @TargetType)
ORDER BY CreatedAt DESC;
""",
            new
            {
                TenantId = _tenantContext.TenantId,
                Status = (int?)status,
                TargetType = (int?)targetType
            },
            session.Transaction);
    }

    public ContentReportDto Submit(CreateContentReportRequest request, int reportedByUserId)
    {
        if (request.TargetId <= 0)
            throw new ValidationException("A valid target must be specified.");

        using var session = new DbSession(_factory, _tenantContext.TenantId);

        var reportCount = session.Connection.ExecuteScalar<int>(
            "SELECT COUNT(1) FROM dbo.ContentReports WHERE TenantId = @TenantId AND TargetType = @TargetType AND TargetId = @TargetId;",
            new { TenantId = _tenantContext.TenantId, TargetType = (int)request.TargetType, TargetId = request.TargetId },
            session.Transaction);

        var autoFlagged = (reportCount + 1) >= AutoFlagThreshold;
        var now = DateTime.UtcNow;

        var id = session.Connection.ExecuteScalar<int>(
            """
INSERT INTO dbo.ContentReports (TenantId, TargetType, TargetId, ReportedByUserId, Category, Details, Status, AutoFlagged, CreatedAt)
VALUES (@TenantId, @TargetType, @TargetId, @ReportedByUserId, @Category, @Details, 1, @AutoFlagged, @CreatedAt);
SELECT SCOPE_IDENTITY();
""",
            new
            {
                TenantId = _tenantContext.TenantId,
                TargetType = (int)request.TargetType,
                TargetId = request.TargetId,
                ReportedByUserId = reportedByUserId,
                Category = (int)request.Category,
                Details = request.Details,
                AutoFlagged = autoFlagged,
                CreatedAt = now
            },
            session.Transaction);

        session.Commit();

        return new ContentReportDto
        {
            Id = id,
            TenantId = _tenantContext.TenantId,
            TargetType = request.TargetType,
            TargetId = request.TargetId,
            ReportedByUserId = reportedByUserId,
            Category = request.Category,
            CategoryLabel = request.Category.ToString(),
            Details = request.Details,
            Status = ReportStatus.Pending,
            StatusLabel = "Pending",
            AutoFlagged = autoFlagged,
            CreatedAt = now
        };
    }

    public void UpdateStatus(int reportId, ReportStatus newStatus)
    {
        using var session = new DbSession(_factory, _tenantContext.TenantId);

        session.Connection.Execute(
            "UPDATE dbo.ContentReports SET Status = @Status, ModifiedAt = @Now WHERE Id = @Id AND TenantId = @TenantId;",
            new { Id = reportId, Status = (int)newStatus, Now = DateTime.UtcNow, TenantId = _tenantContext.TenantId },
            session.Transaction);

        session.Commit();
    }
}
