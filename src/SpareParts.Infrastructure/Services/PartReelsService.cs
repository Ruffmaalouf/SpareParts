using System.ComponentModel.DataAnnotations;
using Dapper;
using SpareParts.Domain.Marketplace;
using SpareParts.Infrastructure.Data;
using SpareParts.Infrastructure.Interfaces;

namespace SpareParts.Infrastructure.Services;

public sealed class PartReelsService
{
    private readonly ISqlConnectionFactory _factory;
    private readonly ITenantContext _tenantContext;

    public PartReelsService(ISqlConnectionFactory factory, ITenantContext tenantContext)
    {
        _factory = factory;
        _tenantContext = tenantContext;
    }

    public (IEnumerable<PartReelDto> Items, int TotalCount) GetAll(int page, int pageSize, PartReelStatus? status = null)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var offset = (page - 1) * pageSize;

        using var session = new DbSession(_factory, _tenantContext.TenantId);
        using var multi = session.Connection.QueryMultiple(
            """
SELECT pr.Id, pr.TenantId, pr.PartId, pr.Title, pr.Description, pr.VideoUrl,
       pr.ThumbnailUrl, pr.PartName, pr.Price, pr.Currency, pr.Status,
       pr.ViewCount, pr.LikeCount, pr.CreatedAt, t.Name AS SellerName
FROM dbo.PartReels pr
LEFT JOIN dbo.Tenants t ON t.Id = pr.TenantId
WHERE (@TenantId = 0 OR pr.TenantId = @TenantId)
  AND (@Status IS NULL OR pr.Status = @Status)
ORDER BY pr.CreatedAt DESC
OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;

SELECT COUNT(1) FROM dbo.PartReels
WHERE (@TenantId = 0 OR TenantId = @TenantId)
  AND (@Status IS NULL OR Status = @Status);
""",
            new { TenantId = _tenantContext.TenantId, Status = (int?)status, Offset = offset, PageSize = pageSize },
            transaction: session.Transaction);

        var items = multi.Read<PartReelDto>().ToList();
        var total = multi.ReadSingle<int>();
        session.Commit();
        return (items, total);
    }

    public PartReelDto? GetById(int id)
    {
        using var session = new DbSession(_factory, _tenantContext.TenantId);
        var item = session.Connection.QueryFirstOrDefault<PartReelDto>(
            """
SELECT pr.Id, pr.TenantId, pr.PartId, pr.Title, pr.Description, pr.VideoUrl,
       pr.ThumbnailUrl, pr.PartName, pr.Price, pr.Currency, pr.Status,
       pr.ViewCount, pr.LikeCount, pr.CreatedAt, t.Name AS SellerName
FROM dbo.PartReels pr
LEFT JOIN dbo.Tenants t ON t.Id = pr.TenantId
WHERE pr.Id = @Id AND (@TenantId = 0 OR pr.TenantId = @TenantId);
""",
            new { Id = id, TenantId = _tenantContext.TenantId },
            transaction: session.Transaction);
        session.Commit();
        return item;
    }

    public int Create(CreatePartReelRequest request, int createdByUserId)
    {
        if (string.IsNullOrWhiteSpace(request.VideoUrl))
            throw new ValidationException("Video URL is required.");

        using var session = new DbSession(_factory, _tenantContext.TenantId);
        var id = session.Connection.QuerySingle<int>(
            """
INSERT INTO dbo.PartReels
    (TenantId, PartId, Title, Description, VideoUrl, ThumbnailUrl, PartName,
     Price, Currency, Status, ViewCount, LikeCount, CreatedAt, CreatedByUserId)
VALUES
    (@TenantId, @PartId, @Title, @Description, @VideoUrl, @ThumbnailUrl, @PartName,
     @Price, @Currency, @Status, 0, 0, SYSUTCDATETIME(), @CreatedByUserId);
SELECT SCOPE_IDENTITY();
""",
            new
            {
                TenantId = _tenantContext.TenantId,
                request.PartId,
                request.Title,
                request.Description,
                request.VideoUrl,
                request.ThumbnailUrl,
                request.PartName,
                request.Price,
                Currency = request.Currency ?? "USD",
                Status = (int)PartReelStatus.Active,
                CreatedByUserId = createdByUserId
            },
            transaction: session.Transaction);
        session.Commit();
        return id;
    }

    public void IncrementView(int id)
    {
        using var conn = _factory.CreateConnection();
        conn.Execute("UPDATE dbo.PartReels SET ViewCount = ViewCount + 1 WHERE Id = @Id;", new { Id = id });
    }

    public void IncrementLike(int id)
    {
        using var conn = _factory.CreateConnection();
        conn.Execute("UPDATE dbo.PartReels SET LikeCount = LikeCount + 1 WHERE Id = @Id;", new { Id = id });
    }

    public void Archive(int id, int modifiedByUserId)
    {
        using var session = new DbSession(_factory, _tenantContext.TenantId);
        session.Connection.Execute(
            "UPDATE dbo.PartReels SET Status = @Status, ModifiedAt = SYSUTCDATETIME(), ModifiedByUserId = @ModifiedByUserId WHERE Id = @Id AND (@TenantId = 0 OR TenantId = @TenantId);",
            new { Id = id, Status = (int)PartReelStatus.Archived, TenantId = _tenantContext.TenantId, ModifiedByUserId = modifiedByUserId },
            transaction: session.Transaction);
        session.Commit();
    }
}
