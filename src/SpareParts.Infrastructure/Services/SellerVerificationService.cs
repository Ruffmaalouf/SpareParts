using System.ComponentModel.DataAnnotations;
using Dapper;
using SpareParts.Domain.Verification;
using SpareParts.Infrastructure.Data;
using SpareParts.Infrastructure.Interfaces;

namespace SpareParts.Infrastructure.Services;

public sealed class SellerVerificationService
{
    private readonly ISqlConnectionFactory _factory;
    private readonly ITenantContext _tenantContext;

    public SellerVerificationService(ISqlConnectionFactory factory, ITenantContext tenantContext)
    {
        _factory = factory;
        _tenantContext = tenantContext;
    }

    public SellerVerificationDto Get(int tenantId)
    {
        using var session = new DbSession(_factory);
        var dto = session.Connection.QueryFirstOrDefault<SellerVerificationDto>(
            """
SELECT TenantId, Status, BusinessName, RegistrationNumber, PhoneVerified,
       IdDocumentUrl, BusinessDocumentUrl, PhysicalAddress,
       AdminNotes, VerifiedAt, SubmittedAt
FROM dbo.SellerVerifications
WHERE TenantId = @TenantId
""",
            new { TenantId = tenantId },
            session.Transaction);

        return dto ?? new SellerVerificationDto { TenantId = tenantId, Status = SellerVerificationStatus.Unverified };
    }

    public void Submit(int tenantId, UpdateSellerVerificationRequest request, int submittedByUserId)
    {
        using var session = new DbSession(_factory);

        var exists = session.Connection.ExecuteScalar<int>(
            "SELECT COUNT(1) FROM dbo.SellerVerifications WHERE TenantId = @TenantId",
            new { TenantId = tenantId },
            session.Transaction) > 0;

        if (exists)
        {
            session.Connection.Execute(
                """
UPDATE dbo.SellerVerifications
SET BusinessName = @BusinessName,
    RegistrationNumber = @RegistrationNumber,
    PhysicalAddress = @PhysicalAddress,
    IdDocumentUrl = @IdDocumentUrl,
    BusinessDocumentUrl = @BusinessDocumentUrl,
    Status = @Status,
    SubmittedAt = SYSUTCDATETIME(),
    ModifiedAt = SYSUTCDATETIME(),
    ModifiedByUserId = @ModifiedByUserId
WHERE TenantId = @TenantId
""",
                new
                {
                    TenantId = tenantId,
                    BusinessName = request.BusinessName?.Trim(),
                    RegistrationNumber = request.RegistrationNumber?.Trim(),
                    PhysicalAddress = request.PhysicalAddress?.Trim(),
                    IdDocumentUrl = request.IdDocumentUrl?.Trim(),
                    BusinessDocumentUrl = request.BusinessDocumentUrl?.Trim(),
                    Status = (int)SellerVerificationStatus.PendingReview,
                    ModifiedByUserId = submittedByUserId
                },
                session.Transaction);
        }
        else
        {
            session.Connection.Execute(
                """
INSERT INTO dbo.SellerVerifications
    (TenantId, Status, BusinessName, RegistrationNumber, PhysicalAddress,
     IdDocumentUrl, BusinessDocumentUrl, SubmittedAt, CreatedAt, CreatedByUserId)
VALUES
    (@TenantId, @Status, @BusinessName, @RegistrationNumber, @PhysicalAddress,
     @IdDocumentUrl, @BusinessDocumentUrl, SYSUTCDATETIME(), SYSUTCDATETIME(), @CreatedByUserId)
""",
                new
                {
                    TenantId = tenantId,
                    Status = (int)SellerVerificationStatus.PendingReview,
                    BusinessName = request.BusinessName?.Trim(),
                    RegistrationNumber = request.RegistrationNumber?.Trim(),
                    PhysicalAddress = request.PhysicalAddress?.Trim(),
                    IdDocumentUrl = request.IdDocumentUrl?.Trim(),
                    BusinessDocumentUrl = request.BusinessDocumentUrl?.Trim(),
                    CreatedByUserId = submittedByUserId
                },
                session.Transaction);
        }

        session.Commit();
    }

    public void AdminDecide(AdminVerificationDecisionRequest request, int adminUserId)
    {
        if (request.Decision == SellerVerificationStatus.PendingReview)
            throw new ValidationException("Decision must be Verified, Rejected, or Suspended.");

        using var session = new DbSession(_factory);
        var affected = session.Connection.Execute(
            """
UPDATE dbo.SellerVerifications
SET Status = @Status,
    AdminNotes = @AdminNotes,
    VerifiedAt = CASE WHEN @Status = 2 THEN SYSUTCDATETIME() ELSE VerifiedAt END,
    ModifiedAt = SYSUTCDATETIME(),
    ModifiedByUserId = @ModifiedByUserId
WHERE TenantId = @TenantId
""",
            new
            {
                TenantId = request.TenantId,
                Status = (int)request.Decision,
                AdminNotes = request.AdminNotes?.Trim(),
                ModifiedByUserId = adminUserId
            },
            session.Transaction);

        if (affected == 0)
            throw new ValidationException($"No verification record found for tenant {request.TenantId}.");

        session.Commit();
    }

    public IEnumerable<SellerVerificationDto> GetPending()
    {
        using var session = new DbSession(_factory);
        return session.Connection.Query<SellerVerificationDto>(
            """
SELECT TenantId, Status, BusinessName, RegistrationNumber, PhoneVerified,
       IdDocumentUrl, BusinessDocumentUrl, PhysicalAddress,
       AdminNotes, VerifiedAt, SubmittedAt
FROM dbo.SellerVerifications
WHERE Status = 1
ORDER BY SubmittedAt ASC
""",
            transaction: session.Transaction).ToList();
    }
}
