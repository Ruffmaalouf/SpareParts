using System.ComponentModel.DataAnnotations;
using Dapper;
using SpareParts.Domain.Passport;
using SpareParts.Infrastructure.Data;
using SpareParts.Infrastructure.Interfaces;

namespace SpareParts.Infrastructure.Services;

public sealed class PartPassportService
{
    private readonly ISqlConnectionFactory _factory;
    private readonly ITenantContext _tenantContext;

    public PartPassportService(ISqlConnectionFactory factory, ITenantContext tenantContext)
    {
        _factory = factory;
        _tenantContext = tenantContext;
    }

    public PartPassportDto? GetPassport(int partId)
    {
        using var session = new DbSession(_factory, _tenantContext.TenantId);

        var partRow = session.Connection.QuerySingleOrDefault<PartNameRow>(
            "SELECT Id, Name, InternalCode FROM dbo.Parts WHERE Id = @PartId AND TenantId = @TenantId;",
            new { PartId = partId, TenantId = _tenantContext.TenantId },
            session.Transaction);

        if (partRow is null) return null;

        var photos = session.Connection.Query<PartPassportPhotoDto>(
            """
SELECT
    Id, PartId, TransactionId, PhotoType,
    CASE PhotoType WHEN 1 THEN 'SellerListing' WHEN 2 THEN 'BuyerReceipt' WHEN 3 THEN 'DisputeEvidence' ELSE 'Unknown' END AS PhotoTypeLabel,
    PhotoUrl, Notes, CreatedAt
FROM dbo.PartPassportPhotos
WHERE PartId = @PartId
ORDER BY CreatedAt ASC;
""",
            new { PartId = partId },
            session.Transaction).ToList();

        return new PartPassportDto
        {
            PartId = partId,
            PartName = partRow.Name,
            InternalCode = partRow.InternalCode,
            Photos = photos
        };
    }

    public PartPassportPhotoDto AddPhoto(AddPassportPhotoRequest request, int createdByUserId)
    {
        if (request.PartId <= 0)
            throw new ValidationException("A valid part must be specified.");
        if (string.IsNullOrWhiteSpace(request.PhotoUrl))
            throw new ValidationException("Photo URL is required.");

        var now = DateTime.UtcNow;

        using var session = new DbSession(_factory, _tenantContext.TenantId);

        var id = session.Connection.ExecuteScalar<int>(
            """
INSERT INTO dbo.PartPassportPhotos (PartId, TransactionId, PhotoType, PhotoUrl, Notes, CreatedAt, CreatedByUserId)
VALUES (@PartId, @TransactionId, @PhotoType, @PhotoUrl, @Notes, @CreatedAt, @CreatedByUserId);
SELECT SCOPE_IDENTITY();
""",
            new
            {
                PartId = request.PartId,
                TransactionId = request.TransactionId,
                PhotoType = (int)request.PhotoType,
                PhotoUrl = request.PhotoUrl,
                Notes = request.Notes,
                CreatedAt = now,
                CreatedByUserId = createdByUserId
            },
            session.Transaction);

        session.Commit();

        return new PartPassportPhotoDto
        {
            Id = id,
            PartId = request.PartId,
            TransactionId = request.TransactionId,
            PhotoType = request.PhotoType,
            PhotoTypeLabel = request.PhotoType.ToString(),
            PhotoUrl = request.PhotoUrl,
            Notes = request.Notes,
            CreatedAt = now
        };
    }
}
