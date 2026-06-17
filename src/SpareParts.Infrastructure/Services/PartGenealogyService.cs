using System.ComponentModel.DataAnnotations;
using Dapper;
using SpareParts.Domain.Genealogy;
using SpareParts.Infrastructure.Data;
using SpareParts.Infrastructure.Interfaces;

namespace SpareParts.Infrastructure.Services;

public sealed class PartGenealogyService
{
    private readonly ISqlConnectionFactory _factory;
    private readonly ITenantContext _tenantContext;

    public PartGenealogyService(ISqlConnectionFactory factory, ITenantContext tenantContext)
    {
        _factory = factory;
        _tenantContext = tenantContext;
    }

    public PartGenealogyDto? GetGenealogy(int partId)
    {
        using var session = new DbSession(_factory, _tenantContext.TenantId);

        var part = session.Connection.QuerySingleOrDefault<PartNameRow>(
            "SELECT Id, Name, InternalCode FROM dbo.Parts WHERE Id = @PartId AND TenantId = @TenantId;",
            new { PartId = partId, TenantId = _tenantContext.TenantId },
            session.Transaction);

        if (part is null) return null;

        var events = session.Connection.Query<PartGenealogyEventDto>(
            """
SELECT
    Id, PartId, EventType,
    CASE EventType
        WHEN 1 THEN 'Dismantled' WHEN 2 THEN 'Listed' WHEN 3 THEN 'Sold'
        WHEN 4 THEN 'Resold' WHEN 5 THEN 'Inspected' WHEN 6 THEN 'Certified'
        WHEN 7 THEN 'Disputed' WHEN 8 THEN 'Repaired' ELSE 'Unknown'
    END AS EventTypeLabel,
    Description, ActorName, TransactionRef, OccurredAt, CreatedAt
FROM dbo.PartGenealogyEvents
WHERE PartId = @PartId
ORDER BY COALESCE(OccurredAt, CreatedAt) ASC;
""",
            new { PartId = partId },
            session.Transaction).ToList();

        return new PartGenealogyDto
        {
            PartId = partId,
            PartName = part.Name,
            InternalCode = part.InternalCode,
            Events = events
        };
    }

    public PartGenealogyEventDto AddEvent(AddGenealogyEventRequest request, int createdByUserId)
    {
        if (request.PartId <= 0)
            throw new ValidationException("A valid part must be specified.");

        var now = DateTime.UtcNow;

        using var session = new DbSession(_factory, _tenantContext.TenantId);

        var id = session.Connection.ExecuteScalar<int>(
            """
INSERT INTO dbo.PartGenealogyEvents (PartId, EventType, Description, ActorName, TransactionRef, OccurredAt, CreatedAt, CreatedByUserId)
VALUES (@PartId, @EventType, @Description, @ActorName, @TransactionRef, @OccurredAt, @CreatedAt, @CreatedByUserId);
SELECT SCOPE_IDENTITY();
""",
            new
            {
                PartId = request.PartId,
                EventType = (int)request.EventType,
                Description = request.Description,
                ActorName = request.ActorName,
                TransactionRef = request.TransactionRef,
                OccurredAt = request.OccurredAt,
                CreatedAt = now,
                CreatedByUserId = createdByUserId
            },
            session.Transaction);

        session.Commit();

        return new PartGenealogyEventDto
        {
            Id = id,
            PartId = request.PartId,
            EventType = request.EventType,
            EventTypeLabel = request.EventType.ToString(),
            Description = request.Description,
            ActorName = request.ActorName,
            TransactionRef = request.TransactionRef,
            OccurredAt = request.OccurredAt,
            CreatedAt = now
        };
    }
}
