using Dapper;
using SpareParts.Domain.Reputation;
using SpareParts.Domain.Verification;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Infrastructure.Services;

public sealed class SellerReputationService
{
    private readonly ISqlConnectionFactory _factory;
    private readonly ITenantContext _tenantContext;

    public SellerReputationService(ISqlConnectionFactory factory, ITenantContext tenantContext)
    {
        _factory = factory;
        _tenantContext = tenantContext;
    }

    public SellerReputationDto? GetReputation(int tenantId)
    {
        using var session = new DbSession(_factory, _tenantContext.TenantId);

        var row = session.Connection.QuerySingleOrDefault<SellerDataRow>(
            """
SELECT
    t.Id      AS TenantId,
    t.Name    AS TenantName,
    -- Active parts count acts as a proxy for seller activity until
    -- a dedicated reviews / disputes table is available.
    ActivePartsCount = (
        SELECT COUNT(1)
        FROM dbo.Parts p
        WHERE p.TenantId = t.Id
          AND p.IsActive = 1
    ),
    -- Return-rate proxy: stock movements with negative quantity relative to total
    ReturnMovements = (
        SELECT COUNT(1)
        FROM dbo.StockMovements sm
        INNER JOIN dbo.Parts p ON p.Id = sm.PartId
        WHERE p.TenantId = t.Id
          AND sm.Quantity < 0
          AND UPPER(ISNULL(CONVERT(NVARCHAR(50), sm.ReferenceType), '')) = N'RETURN'
    ),
    TotalMovements = (
        SELECT COUNT(1)
        FROM dbo.StockMovements sm
        INNER JOIN dbo.Parts p ON p.Id = sm.PartId
        WHERE p.TenantId = t.Id
    ),
    sv.Status AS VerificationStatus
FROM dbo.Tenants t
LEFT JOIN dbo.SellerVerifications sv ON sv.TenantId = t.Id
WHERE t.Id = @TenantId;
""",
            new { TenantId = tenantId },
            session.Transaction);

        if (row is null)
            return null;

        return BuildReputation(row);
    }

    public IEnumerable<SellerReputationDto> GetAll()
    {
        using var session = new DbSession(_factory, _tenantContext.TenantId);

        var rows = session.Connection.Query<SellerDataRow>(
            """
SELECT
    t.Id      AS TenantId,
    t.Name    AS TenantName,
    ActivePartsCount = (
        SELECT COUNT(1)
        FROM dbo.Parts p
        WHERE p.TenantId = t.Id
          AND p.IsActive = 1
    ),
    ReturnMovements = (
        SELECT COUNT(1)
        FROM dbo.StockMovements sm
        INNER JOIN dbo.Parts p ON p.Id = sm.PartId
        WHERE p.TenantId = t.Id
          AND sm.Quantity < 0
          AND UPPER(ISNULL(CONVERT(NVARCHAR(50), sm.ReferenceType), '')) = N'RETURN'
    ),
    TotalMovements = (
        SELECT COUNT(1)
        FROM dbo.StockMovements sm
        INNER JOIN dbo.Parts p ON p.Id = sm.PartId
        WHERE p.TenantId = t.Id
    ),
    sv.Status AS VerificationStatus
FROM dbo.Tenants t
LEFT JOIN dbo.SellerVerifications sv ON sv.TenantId = t.Id
ORDER BY t.Name;
""",
            transaction: session.Transaction)
            .ToList();

        return rows.Select(BuildReputation).ToList();
    }

    private static SellerReputationDto BuildReputation(SellerDataRow row)
    {
        var isVerified = row.VerificationStatus == (int)SellerVerificationStatus.Verified;

        // Base score
        decimal score = 50m;

        // Activity component: up to +20 based on active parts (proxy for seller size/experience)
        // until a dedicated reviews table is available.
        var activityScore = row.ActivePartsCount >= 50 ? 20m : row.ActivePartsCount >= 20 ? 15m : row.ActivePartsCount >= 5 ? 10m : row.ActivePartsCount >= 1 ? 5m : 0m;
        score += activityScore;

        // Return / fulfillment rate component: up to +15
        var returnRate = row.TotalMovements > 0 ? (decimal)row.ReturnMovements / row.TotalMovements : 0m;
        var fulfillmentRate = Math.Max(0m, 1m - returnRate);
        var fulfillmentScore = returnRate == 0m ? 15m : returnRate <= 0.02m ? 10m : returnRate <= 0.05m ? 5m : 0m;
        score += fulfillmentScore;

        // Low-dispute proxy: if activity is present assume 0 disputes until disputes table exists
        var disputeRate = 0m;
        score += 15m; // full dispute-free credit as placeholder

        // Verified seller bonus: +10
        if (isVerified)
            score += 10m;

        score = Math.Min(100m, Math.Max(0m, score));

        // Badges
        var badges = new List<string>();
        if (isVerified)
            badges.Add("Verified Seller");
        if (row.ActivePartsCount > 20)
            badges.Add("Top Seller");

        // Mock average response time — verified sellers are assumed faster
        var avgResponseMinutes = isVerified ? 45 : 120;
        if (avgResponseMinutes < 60)
            badges.Add("Fast Responder");

        return new SellerReputationDto
        {
            TenantId = row.TenantId,
            TenantName = row.TenantName,
            ReputationScore = Math.Round(score, 1),
            ReviewCount = row.ActivePartsCount,
            AverageResponseTimeMinutes = avgResponseMinutes,
            FulfillmentRate = Math.Round(fulfillmentRate * 100m, 1),
            DisputeRate = Math.Round(disputeRate * 100m, 1),
            ReturnRate = Math.Round(returnRate * 100m, 1),
            IsVerified = isVerified,
            Badges = badges,
            LastCalculatedAt = DateTime.UtcNow
        };
    }
}
