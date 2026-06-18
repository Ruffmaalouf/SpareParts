using System.ComponentModel.DataAnnotations;
using Dapper;
using SpareParts.Domain.Marketplace;
using SpareParts.Infrastructure.Data;
using SpareParts.Infrastructure.Interfaces;

namespace SpareParts.Infrastructure.Services;

public sealed class HalfCutService
{
    private readonly ISqlConnectionFactory _factory;
    private readonly ITenantContext _tenantContext;

    public HalfCutService(ISqlConnectionFactory factory, ITenantContext tenantContext)
    {
        _factory = factory;
        _tenantContext = tenantContext;
    }

    public (IEnumerable<HalfCutListingDto> Items, int TotalCount) GetAll(int page, int pageSize, HalfCutStatus? status = null)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var offset = (page - 1) * pageSize;

        using var session = new DbSession(_factory, _tenantContext.TenantId);
        using var multi = session.Connection.QueryMultiple(
            """
SELECT h.Id, h.TenantId, h.VehicleMake, h.VehicleModel, h.VehicleYear, h.VinNumber,
       h.Color, h.OriginCountry, h.Mileage, h.Notes, h.PhotoUrls, h.AskingPrice,
       h.Currency, h.Status,
       CASE h.Status WHEN 1 THEN 'Available' WHEN 2 THEN 'PartlyClaimed' WHEN 3 THEN 'FullyClaimed' WHEN 4 THEN 'Sold' ELSE 'Unknown' END AS StatusLabel,
       h.ClaimsCount, h.CreatedAt, t.Name AS SellerName
FROM dbo.HalfCutListings h
LEFT JOIN dbo.Tenants t ON t.Id = h.TenantId
WHERE (@TenantId = 0 OR h.TenantId = @TenantId)
  AND (@Status IS NULL OR h.Status = @Status)
ORDER BY h.CreatedAt DESC
OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;

SELECT COUNT(1) FROM dbo.HalfCutListings
WHERE (@TenantId = 0 OR TenantId = @TenantId)
  AND (@Status IS NULL OR Status = @Status);
""",
            new { TenantId = _tenantContext.TenantId, Status = (int?)status, Offset = offset, PageSize = pageSize },
            transaction: session.Transaction);

        var items = multi.Read<HalfCutListingDto>().ToList();
        var total = multi.ReadSingle<int>();
        session.Commit();
        return (items, total);
    }

    public HalfCutListingDto? GetById(int id)
    {
        using var session = new DbSession(_factory, _tenantContext.TenantId);
        var item = session.Connection.QueryFirstOrDefault<HalfCutListingDto>(
            """
SELECT h.Id, h.TenantId, h.VehicleMake, h.VehicleModel, h.VehicleYear, h.VinNumber,
       h.Color, h.OriginCountry, h.Mileage, h.Notes, h.PhotoUrls, h.AskingPrice,
       h.Currency, h.Status,
       CASE h.Status WHEN 1 THEN 'Available' WHEN 2 THEN 'PartlyClaimed' WHEN 3 THEN 'FullyClaimed' WHEN 4 THEN 'Sold' ELSE 'Unknown' END AS StatusLabel,
       h.ClaimsCount, h.CreatedAt, t.Name AS SellerName
FROM dbo.HalfCutListings h
LEFT JOIN dbo.Tenants t ON t.Id = h.TenantId
WHERE h.Id = @Id AND (@TenantId = 0 OR h.TenantId = @TenantId);
""",
            new { Id = id, TenantId = _tenantContext.TenantId },
            transaction: session.Transaction);
        session.Commit();
        return item;
    }

    public IEnumerable<HalfCutPartClaimDto> GetClaims(int listingId)
    {
        using var session = new DbSession(_factory, _tenantContext.TenantId);
        var claims = session.Connection.Query<HalfCutPartClaimDto>(
            """
SELECT Id, HalfCutListingId, TenantId, ClaimantUserId, ClaimantName, ClaimantPhone,
       PartName, Notes, DepositAmount, Currency, IsConfirmed, CreatedAt
FROM dbo.HalfCutPartClaims
WHERE HalfCutListingId = @ListingId
ORDER BY CreatedAt DESC;
""",
            new { ListingId = listingId },
            transaction: session.Transaction).ToList();
        session.Commit();
        return claims;
    }

    public int CreateListing(CreateHalfCutListingRequest request, int createdByUserId)
    {
        if (string.IsNullOrWhiteSpace(request.VehicleMake))
            throw new ValidationException("Vehicle make is required.");

        using var session = new DbSession(_factory, _tenantContext.TenantId);
        var id = session.Connection.QuerySingle<int>(
            """
INSERT INTO dbo.HalfCutListings
    (TenantId, VehicleMake, VehicleModel, VehicleYear, VinNumber, Color, OriginCountry,
     Mileage, Notes, PhotoUrls, AskingPrice, Currency, Status, ClaimsCount, CreatedAt, CreatedByUserId)
VALUES
    (@TenantId, @VehicleMake, @VehicleModel, @VehicleYear, @VinNumber, @Color, @OriginCountry,
     @Mileage, @Notes, @PhotoUrls, @AskingPrice, @Currency, @Status, 0, SYSUTCDATETIME(), @CreatedByUserId);
SELECT SCOPE_IDENTITY();
""",
            new
            {
                TenantId = _tenantContext.TenantId,
                request.VehicleMake,
                request.VehicleModel,
                request.VehicleYear,
                request.VinNumber,
                request.Color,
                request.OriginCountry,
                request.Mileage,
                request.Notes,
                request.PhotoUrls,
                request.AskingPrice,
                Currency = request.Currency ?? "USD",
                Status = (int)HalfCutStatus.Available,
                CreatedByUserId = createdByUserId
            },
            transaction: session.Transaction);
        session.Commit();
        return id;
    }

    public int CreateClaim(CreateHalfCutPartClaimRequest request, int createdByUserId)
    {
        using var session = new DbSession(_factory, _tenantContext.TenantId);
        var id = session.Connection.QuerySingle<int>(
            """
INSERT INTO dbo.HalfCutPartClaims
    (HalfCutListingId, TenantId, ClaimantUserId, ClaimantName, ClaimantPhone, PartName,
     Notes, DepositAmount, Currency, IsConfirmed, CreatedAt, CreatedByUserId)
VALUES
    (@HalfCutListingId, @TenantId, NULL, @ClaimantName, @ClaimantPhone, @PartName,
     @Notes, @DepositAmount, @Currency, 0, SYSUTCDATETIME(), @CreatedByUserId);
SELECT SCOPE_IDENTITY();

UPDATE dbo.HalfCutListings SET ClaimsCount = ClaimsCount + 1 WHERE Id = @HalfCutListingId;
""",
            new
            {
                request.HalfCutListingId,
                TenantId = _tenantContext.TenantId,
                request.ClaimantName,
                request.ClaimantPhone,
                request.PartName,
                request.Notes,
                request.DepositAmount,
                Currency = request.Currency ?? "USD",
                CreatedByUserId = createdByUserId
            },
            transaction: session.Transaction);
        session.Commit();
        return id;
    }

    public void ConfirmClaim(int claimId, int modifiedByUserId)
    {
        using var session = new DbSession(_factory, _tenantContext.TenantId);
        session.Connection.Execute(
            "UPDATE dbo.HalfCutPartClaims SET IsConfirmed = 1, ModifiedAt = SYSUTCDATETIME(), ModifiedByUserId = @ModifiedByUserId WHERE Id = @Id;",
            new { Id = claimId, ModifiedByUserId = modifiedByUserId },
            transaction: session.Transaction);
        session.Commit();
    }

    public void UpdateStatus(int id, HalfCutStatus newStatus, int modifiedByUserId)
    {
        using var session = new DbSession(_factory, _tenantContext.TenantId);
        session.Connection.Execute(
            "UPDATE dbo.HalfCutListings SET Status = @Status, ModifiedAt = SYSUTCDATETIME(), ModifiedByUserId = @ModifiedByUserId WHERE Id = @Id AND (@TenantId = 0 OR TenantId = @TenantId);",
            new { Id = id, Status = (int)newStatus, TenantId = _tenantContext.TenantId, ModifiedByUserId = modifiedByUserId },
            transaction: session.Transaction);
        session.Commit();
    }
}
