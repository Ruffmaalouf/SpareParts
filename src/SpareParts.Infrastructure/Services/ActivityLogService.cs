using Dapper;
using SpareParts.Domain.AuditLog;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Infrastructure.Services;

public sealed class ActivityLogService
{
    private readonly ISqlConnectionFactory _factory;

    public ActivityLogService(ISqlConnectionFactory factory)
    {
        _factory = factory;
    }

    public IReadOnlyList<ActivityLogDto> GetLogs(
        string? entityType = null,
        int? entityId = null,
        int? userId = null,
        int page = 1,
        int pageSize = 100)
    {
        using var session = new DbSession(_factory);
        var offset = Math.Max(0, (page - 1) * pageSize);

        return session.Connection.Query<ActivityLogDto>(
            """
SELECT
    al.Id,
    al.Action,
    al.EntityType,
    al.EntityId,
    al.EntityDescription,
    al.OldValues,
    al.NewValues,
    al.UserId,
    al.UserName,
    al.IpAddress,
    al.CreatedAt
FROM dbo.ActivityLogs al
WHERE (@EntityType IS NULL OR al.EntityType = @EntityType)
  AND (@EntityId IS NULL OR al.EntityId = @EntityId)
  AND (@UserId IS NULL OR al.UserId = @UserId)
ORDER BY al.CreatedAt DESC
OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
""",
            new { EntityType = entityType, EntityId = entityId, UserId = userId, Offset = offset, PageSize = pageSize },
            session.Transaction).ToList();
    }

    public void Log(LogActivityRequest req, int? userId, string? userName, string? ipAddress)
    {
        using var session = new DbSession(_factory);
        session.Connection.Execute(
            """
INSERT INTO dbo.ActivityLogs
    (Action, EntityType, EntityId, EntityDescription, OldValues, NewValues, UserId, UserName, IpAddress, CreatedAt)
VALUES
    (@Action, @EntityType, @EntityId, @EntityDescription, @OldValues, @NewValues, @UserId, @UserName, @IpAddress, SYSUTCDATETIME())
""",
            new
            {
                req.Action,
                req.EntityType,
                req.EntityId,
                req.EntityDescription,
                req.OldValues,
                req.NewValues,
                UserId = userId,
                UserName = userName,
                IpAddress = ipAddress
            },
            session.Transaction);
        session.Commit();
    }
}
