using System.ComponentModel.DataAnnotations;
using Dapper;
using SpareParts.Domain.Condition;
using SpareParts.Infrastructure.Data;
using SpareParts.Infrastructure.Interfaces;

namespace SpareParts.Infrastructure.Services;

public sealed class ConditionScannerService
{
    private readonly ISqlConnectionFactory _factory;
    private readonly ITenantContext _tenantContext;

    public ConditionScannerService(ISqlConnectionFactory factory, ITenantContext tenantContext)
    {
        _factory = factory;
        _tenantContext = tenantContext;
    }

    public ConditionCertificateDto Scan(CreateConditionScanRequest request, int createdByUserId)
    {
        if (request.PartId <= 0)
            throw new ValidationException("A valid part must be specified.");
        if (request.PhotoUrls.Count == 0)
            throw new ValidationException("At least one photo is required.");

        var grade = DetermineGrade(request.PhotoUrls.Count, request.Notes);
        var gradeLabel = grade.ToString();
        var certNumber = $"CERT-{request.PartId}-{DateTime.UtcNow:yyyyMMddHHmmss}";
        var photoUrlsJoined = string.Join(",", request.PhotoUrls);
        var now = DateTime.UtcNow;

        using var session = new DbSession(_factory, _tenantContext.TenantId);

        var id = session.Connection.ExecuteScalar<int>(
            """
INSERT INTO dbo.ConditionCertificates
(TenantId, PartId, Grade, GradeLabel, PhotoUrls, Notes, CertificateNumber, ScanMethod, CreatedAt, CreatedByUserId)
VALUES
(@TenantId, @PartId, @Grade, @GradeLabel, @PhotoUrls, @Notes, @CertificateNumber, 'MockAI', @CreatedAt, @CreatedByUserId);
SELECT SCOPE_IDENTITY();
""",
            new
            {
                TenantId = _tenantContext.TenantId,
                PartId = request.PartId,
                Grade = (int)grade,
                GradeLabel = gradeLabel,
                PhotoUrls = photoUrlsJoined,
                Notes = request.Notes,
                CertificateNumber = certNumber,
                CreatedAt = now,
                CreatedByUserId = createdByUserId
            },
            session.Transaction);

        session.Commit();

        return new ConditionCertificateDto
        {
            Id = id,
            TenantId = _tenantContext.TenantId,
            PartId = request.PartId,
            Grade = grade,
            GradeLabel = gradeLabel,
            PhotoUrls = photoUrlsJoined,
            Notes = request.Notes,
            CertificateNumber = certNumber,
            ScanMethod = "MockAI",
            IsExpired = false,
            CreatedAt = now
        };
    }

    public ConditionCertificateDto? GetLatestForPart(int partId)
    {
        using var session = new DbSession(_factory, _tenantContext.TenantId);

        return session.Connection.QueryFirstOrDefault<ConditionCertificateDto>(
            """
SELECT TOP 1
    Id, TenantId, PartId, Grade, GradeLabel, PhotoUrls, Notes,
    CertificateNumber, ScanMethod,
    CASE WHEN DATEDIFF(DAY, CreatedAt, GETUTCDATE()) > 90 THEN 1 ELSE 0 END AS IsExpired,
    CreatedAt
FROM dbo.ConditionCertificates
WHERE PartId = @PartId AND TenantId = @TenantId
ORDER BY CreatedAt DESC;
""",
            new { PartId = partId, TenantId = _tenantContext.TenantId },
            session.Transaction);
    }

    private static ConditionGrade DetermineGrade(int photoCount, string? notes)
    {
        var lowerNotes = (notes ?? string.Empty).ToLowerInvariant();
        if (lowerNotes.Contains("damage") || lowerNotes.Contains("crack") || lowerNotes.Contains("broken"))
            return ConditionGrade.D;
        if (lowerNotes.Contains("wear") || lowerNotes.Contains("scratch") || photoCount == 1)
            return ConditionGrade.C;
        if (photoCount >= 4)
            return ConditionGrade.A;
        return ConditionGrade.B;
    }
}
