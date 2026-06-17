using System.ComponentModel.DataAnnotations;
using Dapper;
using SpareParts.Domain.InstantOffer;
using SpareParts.Infrastructure.Data;
using SpareParts.Infrastructure.Interfaces;

namespace SpareParts.Infrastructure.Services;

public sealed class InstantOfferService
{
    private readonly ISqlConnectionFactory _factory;
    private readonly ITenantContext _tenantContext;
    private readonly MarketPriceIndexService _marketPriceIndex;

    public InstantOfferService(ISqlConnectionFactory factory, ITenantContext tenantContext, MarketPriceIndexService marketPriceIndex)
    {
        _factory = factory;
        _tenantContext = tenantContext;
        _marketPriceIndex = marketPriceIndex;
    }

    public IEnumerable<InstantOfferDto> GetAll(InstantOfferStatus? status = null)
    {
        using var session = new DbSession(_factory, _tenantContext.TenantId);

        return session.Connection.Query<InstantOfferDto>(
            """
SELECT
    Id, TenantId, PartName, PartDescription, PhotoUrls, ConditionGrade,
    EstimatedValue, OfferedPrice, Currency, ContactName, ContactPhone, Status,
    CASE Status WHEN 1 THEN 'Pending' WHEN 2 THEN 'Accepted' WHEN 3 THEN 'Rejected' WHEN 4 THEN 'Contacted' WHEN 5 THEN 'Expired' ELSE 'Unknown' END AS StatusLabel,
    CreatedAt
FROM dbo.InstantOffers
WHERE TenantId = @TenantId
  AND (@Status IS NULL OR Status = @Status)
ORDER BY CreatedAt DESC;
""",
            new { TenantId = _tenantContext.TenantId, Status = (int?)status },
            session.Transaction);
    }

    public InstantOfferDto? GetById(int id)
    {
        using var session = new DbSession(_factory, _tenantContext.TenantId);

        return session.Connection.QuerySingleOrDefault<InstantOfferDto>(
            """
SELECT
    Id, TenantId, PartName, PartDescription, PhotoUrls, ConditionGrade,
    EstimatedValue, OfferedPrice, Currency, ContactName, ContactPhone, Status,
    CASE Status WHEN 1 THEN 'Pending' WHEN 2 THEN 'Accepted' WHEN 3 THEN 'Rejected' WHEN 4 THEN 'Contacted' WHEN 5 THEN 'Expired' ELSE 'Unknown' END AS StatusLabel,
    CreatedAt
FROM dbo.InstantOffers
WHERE Id = @Id AND TenantId = @TenantId;
""",
            new { Id = id, TenantId = _tenantContext.TenantId },
            session.Transaction);
    }

    public InstantOfferDto Submit(SubmitInstantOfferRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.PartName))
            throw new ValidationException("Part name is required.");
        if (string.IsNullOrWhiteSpace(request.ContactName))
            throw new ValidationException("Contact name is required.");

        var currency = request.Currency ?? "USD";
        var index = _marketPriceIndex.GetIndex(request.PartName);
        var estimated = index?.AveragePrice ?? 0m;
        var conditionMultiplier = (request.ConditionGrade ?? "B") switch
        {
            "A" => 0.90m,
            "B" => 0.75m,
            "C" => 0.55m,
            "D" => 0.35m,
            _ => 0.70m
        };
        var offered = Math.Round(estimated * conditionMultiplier, 2);
        var photoUrlsJoined = string.Join(",", request.PhotoUrls);
        var now = DateTime.UtcNow;

        using var session = new DbSession(_factory, _tenantContext.TenantId);

        var id = session.Connection.ExecuteScalar<int>(
            """
INSERT INTO dbo.InstantOffers (TenantId, PartName, PartDescription, PhotoUrls, ConditionGrade, EstimatedValue, OfferedPrice, Currency, ContactName, ContactPhone, Status, CreatedAt)
VALUES (@TenantId, @PartName, @PartDescription, @PhotoUrls, @ConditionGrade, @EstimatedValue, @OfferedPrice, @Currency, @ContactName, @ContactPhone, 1, @CreatedAt);
SELECT SCOPE_IDENTITY();
""",
            new
            {
                TenantId = _tenantContext.TenantId,
                PartName = request.PartName,
                PartDescription = request.PartDescription,
                PhotoUrls = photoUrlsJoined,
                ConditionGrade = request.ConditionGrade,
                EstimatedValue = estimated,
                OfferedPrice = offered,
                Currency = currency,
                ContactName = request.ContactName,
                ContactPhone = request.ContactPhone,
                CreatedAt = now
            },
            session.Transaction);

        session.Commit();

        return new InstantOfferDto
        {
            Id = id,
            TenantId = _tenantContext.TenantId,
            PartName = request.PartName,
            PartDescription = request.PartDescription,
            PhotoUrls = photoUrlsJoined,
            ConditionGrade = request.ConditionGrade,
            EstimatedValue = estimated,
            OfferedPrice = offered,
            Currency = currency,
            ContactName = request.ContactName,
            ContactPhone = request.ContactPhone,
            Status = InstantOfferStatus.Pending,
            StatusLabel = "Pending",
            CreatedAt = now
        };
    }

    public void UpdateStatus(int id, UpdateInstantOfferStatusRequest request)
    {
        using var session = new DbSession(_factory, _tenantContext.TenantId);

        session.Connection.Execute(
            "UPDATE dbo.InstantOffers SET Status = @Status, ModifiedAt = @Now WHERE Id = @Id AND TenantId = @TenantId;",
            new { Id = id, Status = (int)request.NewStatus, Now = DateTime.UtcNow, TenantId = _tenantContext.TenantId },
            session.Transaction);

        session.Commit();
    }
}
