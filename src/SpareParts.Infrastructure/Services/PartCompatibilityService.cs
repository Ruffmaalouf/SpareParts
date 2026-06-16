using System.ComponentModel.DataAnnotations;
using Dapper;
using SpareParts.Domain.Compatibility;
using SpareParts.Infrastructure.Data;
using SpareParts.Infrastructure.Interfaces;

namespace SpareParts.Infrastructure.Services;

public sealed class PartCompatibilityService
{
    private readonly ISqlConnectionFactory _factory;
    private readonly ITenantContext _tenantContext;

    public PartCompatibilityService(ISqlConnectionFactory factory, ITenantContext tenantContext)
    {
        _factory = factory;
        _tenantContext = tenantContext;
    }

    public PartCompatibilityDto? GetByPartId(int partId)
    {
        using var session = new DbSession(_factory, _tenantContext.TenantId);
        return session.Connection.QuerySingleOrDefault<PartCompatibilityDto>(
            """
SELECT
    Id,
    TenantId,
    PartId,
    CompatibleMakes,
    CompatibleModels,
    YearFrom,
    YearTo,
    CompatibleEngines,
    OemNumbers,
    AlternativeNumbers,
    Notes,
    CreatedAt
FROM dbo.PartCompatibilities
WHERE PartId = @PartId
  AND TenantId = @TenantId;
""",
            new { PartId = partId, TenantId = _tenantContext.TenantId },
            session.Transaction);
    }

    public void Upsert(CreatePartCompatibilityRequest request, int modifiedByUserId)
    {
        if (request.PartId <= 0)
            throw new ValidationException("A valid part must be specified.");

        using var session = new DbSession(_factory, _tenantContext.TenantId);

        var existingId = session.Connection.ExecuteScalar<int?>(
            """
SELECT Id FROM dbo.PartCompatibilities
WHERE PartId = @PartId AND TenantId = @TenantId;
""",
            new { PartId = request.PartId, TenantId = _tenantContext.TenantId },
            session.Transaction);

        var now = DateTime.UtcNow;

        if (existingId.HasValue)
        {
            session.Connection.Execute(
                """
UPDATE dbo.PartCompatibilities
SET CompatibleMakes = @CompatibleMakes,
    CompatibleModels = @CompatibleModels,
    YearFrom = @YearFrom,
    YearTo = @YearTo,
    CompatibleEngines = @CompatibleEngines,
    OemNumbers = @OemNumbers,
    AlternativeNumbers = @AlternativeNumbers,
    Notes = @Notes,
    ModifiedAt = @ModifiedAt,
    ModifiedByUserId = @ModifiedByUserId
WHERE Id = @Id;
""",
                new
                {
                    Id = existingId.Value,
                    CompatibleMakes = request.CompatibleMakes,
                    CompatibleModels = request.CompatibleModels,
                    YearFrom = request.YearFrom,
                    YearTo = request.YearTo,
                    CompatibleEngines = request.CompatibleEngines,
                    OemNumbers = request.OemNumbers,
                    AlternativeNumbers = request.AlternativeNumbers,
                    Notes = request.Notes,
                    ModifiedAt = now,
                    ModifiedByUserId = modifiedByUserId
                },
                session.Transaction);
        }
        else
        {
            session.Connection.Execute(
                """
INSERT INTO dbo.PartCompatibilities
(
    TenantId,
    PartId,
    CompatibleMakes,
    CompatibleModels,
    YearFrom,
    YearTo,
    CompatibleEngines,
    OemNumbers,
    AlternativeNumbers,
    Notes,
    CreatedAt,
    CreatedByUserId
)
VALUES
(
    @TenantId,
    @PartId,
    @CompatibleMakes,
    @CompatibleModels,
    @YearFrom,
    @YearTo,
    @CompatibleEngines,
    @OemNumbers,
    @AlternativeNumbers,
    @Notes,
    @CreatedAt,
    @CreatedByUserId
);
""",
                new
                {
                    TenantId = _tenantContext.TenantId,
                    PartId = request.PartId,
                    CompatibleMakes = request.CompatibleMakes,
                    CompatibleModels = request.CompatibleModels,
                    YearFrom = request.YearFrom,
                    YearTo = request.YearTo,
                    CompatibleEngines = request.CompatibleEngines,
                    OemNumbers = request.OemNumbers,
                    AlternativeNumbers = request.AlternativeNumbers,
                    Notes = request.Notes,
                    CreatedAt = now,
                    CreatedByUserId = modifiedByUserId
                },
                session.Transaction);
        }

        session.Commit();
    }

    public CompatibilityCheckResult Check(int partId, string? make, string? model, int? year)
    {
        var dto = GetByPartId(partId);

        var result = new CompatibilityCheckResult { PartId = partId };

        if (dto is null)
        {
            result.Verdict = CompatibilityVerdict.Unknown;
            result.VerdictLabel = "Unknown";
            result.Reason = "No compatibility data available for this part.";
            return result;
        }

        var compatibleMakes = SplitCsv(dto.CompatibleMakes);
        var compatibleModels = SplitCsv(dto.CompatibleModels);
        var oemNumbers = SplitCsv(dto.OemNumbers);

        result.CompatibleMakes = compatibleMakes;
        result.CompatibleModels = compatibleModels;
        result.OemNumbers = oemNumbers;
        result.YearFrom = dto.YearFrom;
        result.YearTo = dto.YearTo;

        if (!string.IsNullOrWhiteSpace(make) && compatibleMakes.Count > 0)
        {
            var makeMatches = compatibleMakes.Any(m => m.Equals(make, StringComparison.OrdinalIgnoreCase));
            if (!makeMatches)
            {
                result.Verdict = CompatibilityVerdict.No;
                result.VerdictLabel = "No";
                result.Reason = $"This part is not compatible with {make}.";
                return result;
            }

            if (!string.IsNullOrWhiteSpace(model) && compatibleModels.Count > 0)
            {
                var modelMatches = compatibleModels.Any(m => m.Equals(model, StringComparison.OrdinalIgnoreCase));
                if (!modelMatches)
                {
                    result.Verdict = CompatibilityVerdict.Probably;
                    result.VerdictLabel = "Probably";
                    result.Reason = $"Make matches but model {model} is not listed in compatible models.";
                    return result;
                }
            }

            if (year.HasValue && (dto.YearFrom.HasValue || dto.YearTo.HasValue))
            {
                var yearOk = (!dto.YearFrom.HasValue || year.Value >= dto.YearFrom.Value)
                          && (!dto.YearTo.HasValue || year.Value <= dto.YearTo.Value);
                if (!yearOk)
                {
                    result.Verdict = CompatibilityVerdict.Probably;
                    result.VerdictLabel = "Probably";
                    result.Reason = $"Make and model match but year {year} is outside the compatible range.";
                    return result;
                }
            }
        }

        result.Verdict = CompatibilityVerdict.Yes;
        result.VerdictLabel = "Yes";
        result.Reason = "Part is compatible based on available data.";
        return result;
    }

    private static List<string> SplitCsv(string? csv)
    {
        if (string.IsNullOrWhiteSpace(csv))
            return new List<string>();
        return csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
    }
}
