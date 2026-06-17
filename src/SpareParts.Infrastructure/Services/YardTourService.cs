using System.ComponentModel.DataAnnotations;
using Dapper;
using SpareParts.Domain.YardTour;
using SpareParts.Infrastructure.Data;
using SpareParts.Infrastructure.Interfaces;

namespace SpareParts.Infrastructure.Services;

public sealed class YardTourService
{
    private readonly ISqlConnectionFactory _factory;
    private readonly ITenantContext _tenantContext;

    public YardTourService(ISqlConnectionFactory factory, ITenantContext tenantContext)
    {
        _factory = factory;
        _tenantContext = tenantContext;
    }

    public IEnumerable<YardTourDto> GetAll(YardTourStatus? status = null)
    {
        using var session = new DbSession(_factory, _tenantContext.TenantId);

        return session.Connection.Query<YardTourDto>(
            """
SELECT
    yt.Id, yt.TenantId,
    t.Name AS SellerName,
    yt.Title, yt.Description, yt.ScheduledAt, yt.StreamUrl, yt.Status,
    CASE yt.Status WHEN 1 THEN 'Scheduled' WHEN 2 THEN 'Live' WHEN 3 THEN 'Completed' WHEN 4 THEN 'Cancelled' ELSE 'Unknown' END AS StatusLabel,
    yt.ViewerCount,
    (SELECT COUNT(1) FROM dbo.YardTourInterests i WHERE i.YardTourId = yt.Id) AS InterestCount,
    yt.CreatedAt
FROM dbo.YardTours yt
LEFT JOIN dbo.Tenants t ON t.Id = yt.TenantId
WHERE (@TenantId = 0 OR yt.TenantId = @TenantId)
  AND (@Status IS NULL OR yt.Status = @Status)
ORDER BY yt.ScheduledAt DESC, yt.CreatedAt DESC;
""",
            new { TenantId = _tenantContext.TenantId, Status = (int?)status },
            session.Transaction);
    }

    public YardTourDto? GetById(int id)
    {
        using var session = new DbSession(_factory, _tenantContext.TenantId);

        return session.Connection.QuerySingleOrDefault<YardTourDto>(
            """
SELECT
    yt.Id, yt.TenantId,
    t.Name AS SellerName,
    yt.Title, yt.Description, yt.ScheduledAt, yt.StreamUrl, yt.Status,
    CASE yt.Status WHEN 1 THEN 'Scheduled' WHEN 2 THEN 'Live' WHEN 3 THEN 'Completed' WHEN 4 THEN 'Cancelled' ELSE 'Unknown' END AS StatusLabel,
    yt.ViewerCount,
    (SELECT COUNT(1) FROM dbo.YardTourInterests i WHERE i.YardTourId = yt.Id) AS InterestCount,
    yt.CreatedAt
FROM dbo.YardTours yt
LEFT JOIN dbo.Tenants t ON t.Id = yt.TenantId
WHERE yt.Id = @Id AND (@TenantId = 0 OR yt.TenantId = @TenantId);
""",
            new { Id = id, TenantId = _tenantContext.TenantId },
            session.Transaction);
    }

    public YardTourDto Create(CreateYardTourRequest request, int createdByUserId)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            throw new ValidationException("Title is required.");

        var now = DateTime.UtcNow;

        using var session = new DbSession(_factory, _tenantContext.TenantId);

        var id = session.Connection.ExecuteScalar<int>(
            """
INSERT INTO dbo.YardTours (TenantId, Title, Description, ScheduledAt, StreamUrl, Status, ViewerCount, CreatedAt, CreatedByUserId)
VALUES (@TenantId, @Title, @Description, @ScheduledAt, @StreamUrl, 1, 0, @CreatedAt, @CreatedByUserId);
SELECT SCOPE_IDENTITY();
""",
            new
            {
                TenantId = _tenantContext.TenantId,
                Title = request.Title,
                Description = request.Description,
                ScheduledAt = request.ScheduledAt,
                StreamUrl = request.StreamUrl,
                CreatedAt = now,
                CreatedByUserId = createdByUserId
            },
            session.Transaction);

        session.Commit();

        return GetById(id)!;
    }

    public void SubmitInterest(int tourId, SubmitYardTourInterestRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ValidationException("Name is required.");

        using var session = new DbSession(_factory, _tenantContext.TenantId);

        session.Connection.Execute(
            """
INSERT INTO dbo.YardTourInterests (YardTourId, Name, Phone, PartRequests, CreatedAt)
VALUES (@YardTourId, @Name, @Phone, @PartRequests, @CreatedAt);
""",
            new
            {
                YardTourId = tourId,
                Name = request.Name,
                Phone = request.Phone,
                PartRequests = request.PartRequests,
                CreatedAt = DateTime.UtcNow
            },
            session.Transaction);

        session.Commit();
    }

    public void UpdateStatus(int id, YardTourStatus newStatus)
    {
        using var session = new DbSession(_factory, _tenantContext.TenantId);

        session.Connection.Execute(
            "UPDATE dbo.YardTours SET Status = @Status WHERE Id = @Id AND TenantId = @TenantId;",
            new { Id = id, Status = (int)newStatus, TenantId = _tenantContext.TenantId },
            session.Transaction);

        session.Commit();
    }
}
