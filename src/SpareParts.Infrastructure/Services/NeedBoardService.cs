using Dapper;
using SpareParts.Domain.NeedBoard;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Infrastructure.Services;

public sealed class NeedBoardService
{
    private readonly ISqlConnectionFactory _factory;
    private readonly ITenantContext _tenantContext;

    public NeedBoardService(ISqlConnectionFactory factory, ITenantContext tenantContext)
    {
        _factory = factory;
        _tenantContext = tenantContext;
    }

    public (IEnumerable<PartWantedAdDto> Items, int TotalCount) GetAll(
        int page,
        int pageSize,
        PartWantedAdStatus? status = null,
        string? vehicleMake = null,
        string? vehicleModel = null,
        string? search = null)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var offset = (page - 1) * pageSize;

        using var session = new DbSession(_factory, _tenantContext.TenantId);

        using var multi = session.Connection.QueryMultiple(
            """
SELECT
    Id,
    TenantId,
    BuyerUserId,
    BuyerName,
    BuyerPhone,
    PartName,
    Notes,
    VehicleMake,
    VehicleModel,
    VehicleYear,
    VinNumber,
    Budget,
    Currency,
    LocationCity,
    LocationCountry,
    Urgency,
    PhotoUrl,
    Status,
    CASE Status WHEN 1 THEN 'Open' WHEN 2 THEN 'Contacted' WHEN 3 THEN 'Fulfilled' WHEN 4 THEN 'Expired' WHEN 5 THEN 'Cancelled' ELSE 'Unknown' END AS StatusLabel,
    ExpiresAt,
    OffersCount,
    CreatedAt,
    CreatedByUserId,
    ModifiedAt,
    ModifiedByUserId
FROM dbo.PartWantedAds
WHERE (@TenantId = 0 OR TenantId = @TenantId)
  AND (@Status IS NULL OR Status = @Status)
  AND (@VehicleMake IS NULL OR VehicleMake = @VehicleMake)
  AND (@VehicleModel IS NULL OR VehicleModel = @VehicleModel)
  AND (@Search IS NULL
       OR PartName LIKE '%' + @Search + '%'
       OR Notes LIKE '%' + @Search + '%'
       OR BuyerName LIKE '%' + @Search + '%')
ORDER BY CreatedAt DESC
OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;

SELECT COUNT(1)
FROM dbo.PartWantedAds
WHERE (@TenantId = 0 OR TenantId = @TenantId)
  AND (@Status IS NULL OR Status = @Status)
  AND (@VehicleMake IS NULL OR VehicleMake = @VehicleMake)
  AND (@VehicleModel IS NULL OR VehicleModel = @VehicleModel)
  AND (@Search IS NULL
       OR PartName LIKE '%' + @Search + '%'
       OR Notes LIKE '%' + @Search + '%'
       OR BuyerName LIKE '%' + @Search + '%');
""",
            new
            {
                TenantId = _tenantContext.TenantId,
                Status = status.HasValue ? (int?)status.Value : null,
                VehicleMake = string.IsNullOrWhiteSpace(vehicleMake) ? null : vehicleMake.Trim(),
                VehicleModel = string.IsNullOrWhiteSpace(vehicleModel) ? null : vehicleModel.Trim(),
                Search = string.IsNullOrWhiteSpace(search) ? null : search.Trim(),
                Offset = offset,
                PageSize = pageSize
            },
            session.Transaction);

        var items = multi.Read<PartWantedAdDto>().ToList();
        var totalCount = multi.ReadFirst<int>();
        return (items, totalCount);
    }

    public PartWantedAdDto? GetById(int id)
    {
        using var session = new DbSession(_factory, _tenantContext.TenantId);

        var ad = session.Connection.QuerySingleOrDefault<PartWantedAdDto>(
            """
SELECT
    Id,
    TenantId,
    BuyerUserId,
    BuyerName,
    BuyerPhone,
    PartName,
    Notes,
    VehicleMake,
    VehicleModel,
    VehicleYear,
    VinNumber,
    Budget,
    Currency,
    LocationCity,
    LocationCountry,
    Urgency,
    PhotoUrl,
    Status,
    CASE Status WHEN 1 THEN 'Open' WHEN 2 THEN 'Contacted' WHEN 3 THEN 'Fulfilled' WHEN 4 THEN 'Expired' WHEN 5 THEN 'Cancelled' ELSE 'Unknown' END AS StatusLabel,
    ExpiresAt,
    OffersCount,
    CreatedAt,
    CreatedByUserId,
    ModifiedAt,
    ModifiedByUserId
FROM dbo.PartWantedAds
WHERE Id = @Id
  AND (@TenantId = 0 OR TenantId = @TenantId);
""",
            new { Id = id, TenantId = _tenantContext.TenantId },
            session.Transaction);

        return ad;
    }

    public IEnumerable<PartWantedAdOfferDto> GetOffersForAd(int adId)
    {
        using var session = new DbSession(_factory, _tenantContext.TenantId);
        return session.Connection.Query<PartWantedAdOfferDto>(
            """
SELECT
    Id,
    WantedAdId,
    TenantId,
    SellerUserId,
    SellerName,
    SellerPhone,
    PartId,
    OfferedPrice,
    Currency,
    Condition,
    Notes,
    PhotoUrl,
    IsAccepted,
    CreatedAt,
    CreatedByUserId,
    ModifiedAt,
    ModifiedByUserId
FROM dbo.PartWantedAdOffers
WHERE WantedAdId = @AdId
ORDER BY IsAccepted DESC, CreatedAt ASC;
""",
            new { AdId = adId },
            session.Transaction)
            .ToList();
    }

    public int CreateAd(CreatePartWantedAdRequest request, int? buyerUserId, int? createdByUserId)
    {
        if (string.IsNullOrWhiteSpace(request.BuyerName))
            throw new ValidationException("Buyer name is required.");
        if (string.IsNullOrWhiteSpace(request.PartName))
            throw new ValidationException("Part name is required.");

        using var session = new DbSession(_factory, _tenantContext.TenantId);

        DateTime? expiresAt = request.ExpiresInDays.HasValue && request.ExpiresInDays.Value > 0
            ? DateTime.UtcNow.AddDays(request.ExpiresInDays.Value)
            : null;

        var id = session.Connection.ExecuteScalar<int>(
            """
INSERT INTO dbo.PartWantedAds
(
    TenantId,
    BuyerUserId,
    BuyerName,
    BuyerPhone,
    PartName,
    Notes,
    VehicleMake,
    VehicleModel,
    VehicleYear,
    VinNumber,
    Budget,
    Currency,
    LocationCity,
    LocationCountry,
    Urgency,
    Status,
    ExpiresAt,
    OffersCount,
    CreatedAt,
    CreatedByUserId
)
VALUES
(
    @TenantId,
    @BuyerUserId,
    @BuyerName,
    @BuyerPhone,
    @PartName,
    @Notes,
    @VehicleMake,
    @VehicleModel,
    @VehicleYear,
    @VinNumber,
    @Budget,
    @Currency,
    @LocationCity,
    @LocationCountry,
    @Urgency,
    @Status,
    @ExpiresAt,
    0,
    @CreatedAt,
    @CreatedByUserId
);
SELECT CAST(SCOPE_IDENTITY() AS INT);
""",
            new
            {
                TenantId = _tenantContext.TenantId,
                BuyerUserId = buyerUserId,
                BuyerName = request.BuyerName,
                BuyerPhone = request.BuyerPhone,
                PartName = request.PartName,
                Notes = request.Notes,
                VehicleMake = request.VehicleMake,
                VehicleModel = request.VehicleModel,
                VehicleYear = request.VehicleYear,
                VinNumber = request.VinNumber,
                Budget = request.Budget,
                Currency = request.Currency ?? "USD",
                LocationCity = request.LocationCity,
                LocationCountry = request.LocationCountry,
                Urgency = request.Urgency ?? "Normal",
                Status = (int)PartWantedAdStatus.Open,
                ExpiresAt = expiresAt,
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = createdByUserId
            },
            session.Transaction);

        session.Commit();
        return id;
    }

    public int CreateOffer(CreatePartWantedAdOfferRequest request, int? sellerUserId, int? createdByUserId)
    {
        if (string.IsNullOrWhiteSpace(request.SellerName))
            throw new ValidationException("Seller name is required.");
        if (request.WantedAdId <= 0)
            throw new ValidationException("A valid wanted ad must be specified.");

        using var session = new DbSession(_factory, _tenantContext.TenantId);

        var adStatus = session.Connection.ExecuteScalar<int?>(
            "SELECT Status FROM dbo.PartWantedAds WHERE Id = @Id;",
            new { Id = request.WantedAdId },
            session.Transaction);

        if (adStatus is null)
            throw new NotFoundException("Wanted ad not found.");

        if (adStatus != (int)PartWantedAdStatus.Open && adStatus != (int)PartWantedAdStatus.Contacted)
            throw new ValidationException("This wanted ad is no longer accepting offers.");

        var offerId = session.Connection.ExecuteScalar<int>(
            """
INSERT INTO dbo.PartWantedAdOffers
(
    WantedAdId,
    TenantId,
    SellerUserId,
    SellerName,
    SellerPhone,
    PartId,
    OfferedPrice,
    Currency,
    Condition,
    Notes,
    IsAccepted,
    CreatedAt,
    CreatedByUserId
)
VALUES
(
    @WantedAdId,
    @TenantId,
    @SellerUserId,
    @SellerName,
    @SellerPhone,
    @PartId,
    @OfferedPrice,
    @Currency,
    @Condition,
    @Notes,
    0,
    @CreatedAt,
    @CreatedByUserId
);
SELECT CAST(SCOPE_IDENTITY() AS INT);
""",
            new
            {
                WantedAdId = request.WantedAdId,
                TenantId = _tenantContext.TenantId,
                SellerUserId = sellerUserId,
                SellerName = request.SellerName,
                SellerPhone = request.SellerPhone,
                PartId = request.PartId,
                OfferedPrice = request.OfferedPrice,
                Currency = request.Currency ?? "USD",
                Condition = request.Condition,
                Notes = request.Notes,
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = createdByUserId
            },
            session.Transaction);

        session.Connection.Execute(
            """
UPDATE dbo.PartWantedAds
SET OffersCount = OffersCount + 1,
    Status = CASE WHEN Status = @OpenStatus THEN @ContactedStatus ELSE Status END,
    ModifiedAt = @Now
WHERE Id = @AdId;
""",
            new
            {
                AdId = request.WantedAdId,
                OpenStatus = (int)PartWantedAdStatus.Open,
                ContactedStatus = (int)PartWantedAdStatus.Contacted,
                Now = DateTime.UtcNow
            },
            session.Transaction);

        session.Commit();
        return offerId;
    }

    public void AcceptOffer(int offerId, int adId, int? modifiedByUserId)
    {
        using var session = new DbSession(_factory, _tenantContext.TenantId);

        var offerExists = session.Connection.ExecuteScalar<bool>(
            """
SELECT CASE WHEN EXISTS (
    SELECT 1 FROM dbo.PartWantedAdOffers WHERE Id = @OfferId AND WantedAdId = @AdId
) THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END;
""",
            new { OfferId = offerId, AdId = adId },
            session.Transaction);

        if (!offerExists)
            throw new NotFoundException("Offer not found.");

        session.Connection.Execute(
            """
UPDATE dbo.PartWantedAdOffers
SET IsAccepted = 1, ModifiedAt = @Now, ModifiedByUserId = @ModifiedByUserId
WHERE Id = @OfferId;

UPDATE dbo.PartWantedAds
SET Status = @FulfilledStatus,
    ModifiedAt = @Now,
    ModifiedByUserId = @ModifiedByUserId
WHERE Id = @AdId;
""",
            new
            {
                OfferId = offerId,
                AdId = adId,
                FulfilledStatus = (int)PartWantedAdStatus.Fulfilled,
                Now = DateTime.UtcNow,
                ModifiedByUserId = modifiedByUserId
            },
            session.Transaction);

        session.Commit();
    }

    public void CancelAd(int id, int? modifiedByUserId)
    {
        using var session = new DbSession(_factory, _tenantContext.TenantId);

        var exists = session.Connection.ExecuteScalar<bool>(
            """
SELECT CASE WHEN EXISTS (
    SELECT 1 FROM dbo.PartWantedAds WHERE Id = @Id
    AND (@TenantId = 0 OR TenantId = @TenantId)
) THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END;
""",
            new { Id = id, TenantId = _tenantContext.TenantId },
            session.Transaction);

        if (!exists)
            throw new NotFoundException("Wanted ad not found.");

        session.Connection.Execute(
            """
UPDATE dbo.PartWantedAds
SET Status = @CancelledStatus,
    ModifiedAt = @Now,
    ModifiedByUserId = @ModifiedByUserId
WHERE Id = @Id;
""",
            new
            {
                Id = id,
                CancelledStatus = (int)PartWantedAdStatus.Cancelled,
                Now = DateTime.UtcNow,
                ModifiedByUserId = modifiedByUserId
            },
            session.Transaction);

        session.Commit();
    }
}
