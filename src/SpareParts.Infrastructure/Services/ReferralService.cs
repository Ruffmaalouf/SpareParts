using Dapper;
using SpareParts.Domain.Referral;
using SpareParts.Infrastructure.Data;
using SpareParts.Infrastructure.Interfaces;

namespace SpareParts.Infrastructure.Services;

public sealed class ReferralService
{
    private readonly ISqlConnectionFactory _factory;
    private readonly ITenantContext _tenantContext;

    public ReferralService(ISqlConnectionFactory factory, ITenantContext tenantContext)
    {
        _factory = factory;
        _tenantContext = tenantContext;
    }

    public ReferralCodeDto? GetMyCode(int userId)
    {
        using var session = new DbSession(_factory, _tenantContext.TenantId);
        var dto = session.Connection.QuerySingleOrDefault<ReferralCodeDto>(
            """
SELECT
    Id,
    TenantId,
    UserId,
    Code,
    TotalReferrals,
    TotalCreditsEarned,
    Currency,
    IsActive,
    CreatedAt
FROM dbo.ReferralCodes
WHERE UserId = @UserId
  AND (@TenantId = 0 OR TenantId = @TenantId)
  AND IsActive = 1;
""",
            new { UserId = userId, TenantId = _tenantContext.TenantId },
            session.Transaction);

        if (dto is not null)
            dto.ReferralUrl = $"https://sparepartsapp.com/signup?ref={dto.Code}";

        return dto;
    }

    public ReferralCodeDto GenerateCode(int userId, int createdByUserId)
    {
        using var session = new DbSession(_factory, _tenantContext.TenantId);

        var existing = session.Connection.ExecuteScalar<bool>(
            """
SELECT CASE WHEN EXISTS (
    SELECT 1 FROM dbo.ReferralCodes
    WHERE UserId = @UserId AND (@TenantId = 0 OR TenantId = @TenantId) AND IsActive = 1
) THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END;
""",
            new { UserId = userId, TenantId = _tenantContext.TenantId },
            session.Transaction);

        if (existing)
            throw new ValidationException("An active referral code already exists for this user.");

        var code = GenerateUniqueCode();
        var now = DateTime.UtcNow;

        var id = session.Connection.ExecuteScalar<int>(
            """
INSERT INTO dbo.ReferralCodes
(
    TenantId,
    UserId,
    Code,
    TotalReferrals,
    TotalCreditsEarned,
    Currency,
    IsActive,
    CreatedAt,
    CreatedByUserId
)
VALUES
(
    @TenantId,
    @UserId,
    @Code,
    0,
    0,
    'USD',
    1,
    @CreatedAt,
    @CreatedByUserId
);
SELECT CAST(SCOPE_IDENTITY() AS INT);
""",
            new
            {
                TenantId = _tenantContext.TenantId,
                UserId = userId,
                Code = code,
                CreatedAt = now,
                CreatedByUserId = createdByUserId
            },
            session.Transaction);

        session.Commit();

        return new ReferralCodeDto
        {
            Id = id,
            TenantId = _tenantContext.TenantId,
            UserId = userId,
            Code = code,
            TotalReferrals = 0,
            TotalCreditsEarned = 0,
            Currency = "USD",
            IsActive = true,
            CreatedAt = now,
            ReferralUrl = $"https://sparepartsapp.com/signup?ref={code}"
        };
    }

    public IEnumerable<ReferralSignupDto> GetReferrals(int referralCodeId)
    {
        using var session = new DbSession(_factory, _tenantContext.TenantId);
        return session.Connection.Query<ReferralSignupDto>(
            """
SELECT
    Id,
    ReferralCodeId,
    ReferredTenantId,
    ReferredUserId,
    ReferredEmail,
    CreditAmount,
    Currency,
    CreditPaid,
    CreditPaidAt,
    CreatedAt,
    ReferredName
FROM dbo.ReferralSignups
WHERE ReferralCodeId = @ReferralCodeId
ORDER BY CreatedAt DESC;
""",
            new { ReferralCodeId = referralCodeId },
            session.Transaction)
            .ToList();
    }

    public int RecordSignup(int referralCodeId, string email, int? tenantId, int? userId)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ValidationException("Email is required.");

        using var session = new DbSession(_factory, _tenantContext.TenantId);

        var signupId = session.Connection.ExecuteScalar<int>(
            """
INSERT INTO dbo.ReferralSignups
(
    ReferralCodeId,
    ReferredTenantId,
    ReferredUserId,
    ReferredEmail,
    CreditAmount,
    Currency,
    CreditPaid,
    CreatedAt
)
VALUES
(
    @ReferralCodeId,
    @ReferredTenantId,
    @ReferredUserId,
    @ReferredEmail,
    0,
    'USD',
    0,
    @CreatedAt
);
SELECT CAST(SCOPE_IDENTITY() AS INT);
""",
            new
            {
                ReferralCodeId = referralCodeId,
                ReferredTenantId = tenantId,
                ReferredUserId = userId,
                ReferredEmail = email,
                CreatedAt = DateTime.UtcNow
            },
            session.Transaction);

        session.Connection.Execute(
            """
UPDATE dbo.ReferralCodes
SET TotalReferrals = TotalReferrals + 1
WHERE Id = @ReferralCodeId;
""",
            new { ReferralCodeId = referralCodeId },
            session.Transaction);

        session.Commit();
        return signupId;
    }

    private static string GenerateUniqueCode()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();
        var suffix = new string(Enumerable.Range(0, 6).Select(_ => chars[random.Next(chars.Length)]).ToArray());
        return $"SP-{suffix}";
    }
}
