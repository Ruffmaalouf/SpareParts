using System.ComponentModel.DataAnnotations;
using Dapper;
using SpareParts.Domain.Negotiation;
using SpareParts.Infrastructure.Data;
using SpareParts.Infrastructure.Interfaces;

namespace SpareParts.Infrastructure.Services;

public sealed class NegotiationBotService
{
    private readonly ISqlConnectionFactory _factory;
    private readonly ITenantContext _tenantContext;

    public NegotiationBotService(ISqlConnectionFactory factory, ITenantContext tenantContext)
    {
        _factory = factory;
        _tenantContext = tenantContext;
    }

    public IEnumerable<NegotiationSessionDto> GetAll(NegotiationStatus? status = null)
    {
        using var session = new DbSession(_factory, _tenantContext.TenantId);

        var sessions = session.Connection.Query<NegotiationSessionDto>(
            """
SELECT
    n.Id, n.TenantId, n.PartId,
    p.Name AS PartName,
    n.BuyerName, n.BuyerPhone, n.AskingPrice, n.CurrentOffer, n.Status,
    CASE n.Status WHEN 1 THEN 'Open' WHEN 2 THEN 'CounterOffered' WHEN 3 THEN 'Accepted' WHEN 4 THEN 'Rejected' WHEN 5 THEN 'Expired' ELSE 'Unknown' END AS StatusLabel,
    n.Currency, n.CreatedAt
FROM dbo.Negotiations n
LEFT JOIN dbo.Parts p ON p.Id = n.PartId
WHERE (@TenantId = 0 OR n.TenantId = @TenantId)
  AND (@Status IS NULL OR n.Status = @Status)
ORDER BY n.CreatedAt DESC;
""",
            new { TenantId = _tenantContext.TenantId, Status = (int?)status },
            session.Transaction).ToList();

        foreach (var s in sessions)
        {
            s.Offers = session.Connection.Query<NegotiationOfferDto>(
                "SELECT Id, SessionId, OfferedByRole, Amount, Note, CreatedAt FROM dbo.NegotiationOffers WHERE SessionId = @SessionId ORDER BY CreatedAt ASC;",
                new { SessionId = s.Id },
                session.Transaction).ToList();
        }

        return sessions;
    }

    public NegotiationSessionDto? GetById(int id)
    {
        using var session = new DbSession(_factory, _tenantContext.TenantId);

        var nego = session.Connection.QuerySingleOrDefault<NegotiationSessionDto>(
            """
SELECT
    n.Id, n.TenantId, n.PartId,
    p.Name AS PartName,
    n.BuyerName, n.BuyerPhone, n.AskingPrice, n.CurrentOffer, n.Status,
    CASE n.Status WHEN 1 THEN 'Open' WHEN 2 THEN 'CounterOffered' WHEN 3 THEN 'Accepted' WHEN 4 THEN 'Rejected' WHEN 5 THEN 'Expired' ELSE 'Unknown' END AS StatusLabel,
    n.Currency, n.CreatedAt
FROM dbo.Negotiations n
LEFT JOIN dbo.Parts p ON p.Id = n.PartId
WHERE n.Id = @Id AND (@TenantId = 0 OR n.TenantId = @TenantId);
""",
            new { Id = id, TenantId = _tenantContext.TenantId },
            session.Transaction);

        if (nego is null) return null;

        nego.Offers = session.Connection.Query<NegotiationOfferDto>(
            "SELECT Id, SessionId, OfferedByRole, Amount, Note, CreatedAt FROM dbo.NegotiationOffers WHERE SessionId = @SessionId ORDER BY CreatedAt ASC;",
            new { SessionId = id },
            session.Transaction).ToList();

        return nego;
    }

    public NegotiationSessionDto Start(CreateNegotiationRequest request, int createdByUserId)
    {
        if (request.PartId <= 0)
            throw new ValidationException("A valid part must be specified.");
        if (string.IsNullOrWhiteSpace(request.BuyerName))
            throw new ValidationException("Buyer name is required.");
        if (request.InitialOffer <= 0)
            throw new ValidationException("Initial offer must be positive.");

        using var session = new DbSession(_factory, _tenantContext.TenantId);

        var partRow = session.Connection.QuerySingleOrDefault<PartPriceRow>(
            "SELECT Id, Name, SalePrice, Currency FROM dbo.Parts WHERE Id = @PartId AND (@TenantId = 0 OR TenantId = @TenantId);",
            new { PartId = request.PartId, TenantId = _tenantContext.TenantId },
            session.Transaction);

        if (partRow is null)
            throw new NotFoundException($"Part {request.PartId} not found.");

        var currency = request.Currency ?? partRow.Currency ?? "USD";
        var now = DateTime.UtcNow;

        var sessionId = session.Connection.ExecuteScalar<int>(
            """
INSERT INTO dbo.Negotiations (TenantId, PartId, BuyerName, BuyerPhone, AskingPrice, BuyerMaxPrice, CurrentOffer, Status, Currency, CreatedAt, CreatedByUserId)
VALUES (@TenantId, @PartId, @BuyerName, @BuyerPhone, @AskingPrice, @BuyerMaxPrice, @InitialOffer, 1, @Currency, @CreatedAt, @CreatedByUserId);
SELECT SCOPE_IDENTITY();
""",
            new
            {
                TenantId = _tenantContext.TenantId,
                PartId = request.PartId,
                BuyerName = request.BuyerName,
                BuyerPhone = request.BuyerPhone,
                AskingPrice = partRow.SalePrice,
                BuyerMaxPrice = request.BuyerMaxPrice,
                InitialOffer = request.InitialOffer,
                Currency = currency,
                CreatedAt = now,
                CreatedByUserId = createdByUserId
            },
            session.Transaction);

        session.Connection.Execute(
            "INSERT INTO dbo.NegotiationOffers (SessionId, OfferedByRole, Amount, Note, CreatedAt) VALUES (@SessionId, 'Buyer', @Amount, NULL, @CreatedAt);",
            new { SessionId = sessionId, Amount = request.InitialOffer, CreatedAt = now },
            session.Transaction);

        var counterAmount = ComputeCounter(request.InitialOffer, partRow.SalePrice, request.BuyerMaxPrice);
        if (counterAmount.HasValue)
        {
            session.Connection.Execute(
                "INSERT INTO dbo.NegotiationOffers (SessionId, OfferedByRole, Amount, Note, CreatedAt) VALUES (@SessionId, 'Bot', @Amount, 'AI counter-offer', @CreatedAt);",
                new { SessionId = sessionId, Amount = counterAmount.Value, CreatedAt = now.AddSeconds(1) },
                session.Transaction);

            session.Connection.Execute(
                "UPDATE dbo.Negotiations SET Status = 2, CurrentOffer = @Counter WHERE Id = @Id;",
                new { Counter = counterAmount.Value, Id = sessionId },
                session.Transaction);
        }

        session.Commit();

        return GetById(sessionId)!;
    }

    public NegotiationSessionDto MakeOffer(int sessionId, MakeNegotiationOfferRequest request)
    {
        if (request.Amount <= 0)
            throw new ValidationException("Offer amount must be positive.");

        using var session = new DbSession(_factory, _tenantContext.TenantId);

        var nego = session.Connection.QuerySingleOrDefault<NegotiationBotRow>(
            "SELECT Id, AskingPrice, BuyerMaxPrice, SellerMinPrice, Status FROM dbo.Negotiations WHERE Id = @Id AND (@TenantId = 0 OR TenantId = @TenantId);",
            new { Id = sessionId, TenantId = _tenantContext.TenantId },
            session.Transaction);

        if (nego is null) throw new NotFoundException($"Negotiation {sessionId} not found.");
        if (nego.Status is 3 or 4 or 5)
            throw new ValidationException("This negotiation is already closed.");

        var now = DateTime.UtcNow;

        session.Connection.Execute(
            "INSERT INTO dbo.NegotiationOffers (SessionId, OfferedByRole, Amount, Note, CreatedAt) VALUES (@SessionId, @Role, @Amount, @Note, @CreatedAt);",
            new { SessionId = sessionId, Role = request.OfferedByRole, Amount = request.Amount, Note = request.Note, CreatedAt = now },
            session.Transaction);

        session.Connection.Execute(
            "UPDATE dbo.Negotiations SET CurrentOffer = @Amount WHERE Id = @Id;",
            new { Amount = request.Amount, Id = sessionId },
            session.Transaction);

        session.Commit();

        return GetById(sessionId)!;
    }

    public void Accept(int sessionId)
    {
        using var session = new DbSession(_factory, _tenantContext.TenantId);
        session.Connection.Execute(
            "UPDATE dbo.Negotiations SET Status = 3 WHERE Id = @Id AND (@TenantId = 0 OR TenantId = @TenantId);",
            new { Id = sessionId, TenantId = _tenantContext.TenantId }, session.Transaction);
        session.Commit();
    }

    public void Reject(int sessionId)
    {
        using var session = new DbSession(_factory, _tenantContext.TenantId);
        session.Connection.Execute(
            "UPDATE dbo.Negotiations SET Status = 4 WHERE Id = @Id AND (@TenantId = 0 OR TenantId = @TenantId);",
            new { Id = sessionId, TenantId = _tenantContext.TenantId }, session.Transaction);
        session.Commit();
    }

    private static decimal? ComputeCounter(decimal buyerOffer, decimal askingPrice, decimal? buyerMax)
    {
        if (askingPrice <= buyerOffer) return null;
        var midpoint = (buyerMax.HasValue && buyerMax.Value > buyerOffer)
            ? (buyerMax.Value + askingPrice) / 2m
            : (buyerOffer + askingPrice) / 2m;
        return Math.Round(midpoint, 2);
    }
}
