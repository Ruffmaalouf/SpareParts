using System.ComponentModel.DataAnnotations;
using Dapper;
using SpareParts.Domain.Marketplace;
using SpareParts.Infrastructure.Data;
using SpareParts.Infrastructure.Interfaces;

namespace SpareParts.Infrastructure.Services;

public sealed class EscrowService
{
    private readonly ISqlConnectionFactory _factory;
    private readonly ITenantContext _tenantContext;

    public EscrowService(ISqlConnectionFactory factory, ITenantContext tenantContext)
    {
        _factory = factory;
        _tenantContext = tenantContext;
    }

    public (IEnumerable<EscrowTransactionDto> Items, int TotalCount) GetAll(int page, int pageSize, EscrowStatus? status = null)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var offset = (page - 1) * pageSize;

        using var session = new DbSession(_factory, _tenantContext.TenantId);
        using var multi = session.Connection.QueryMultiple(
            """
SELECT Id, TenantId, SalesInvoiceId, PartId, BuyerName, BuyerPhone, SellerName, SellerPhone,
       Amount, Currency, Status, Notes, TrackingNumber, ShippedAt, DeliveredAt,
       ReleasedAt, DisputedAt, DisputeReason, AutoReleaseAfterHours, CreatedAt
FROM dbo.EscrowTransactions
WHERE (@TenantId = 0 OR TenantId = @TenantId)
  AND (@Status IS NULL OR Status = @Status)
ORDER BY CreatedAt DESC
OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;

SELECT COUNT(1) FROM dbo.EscrowTransactions
WHERE (@TenantId = 0 OR TenantId = @TenantId) AND (@Status IS NULL OR Status = @Status);
""",
            new { TenantId = _tenantContext.TenantId, Status = (int?)status, Offset = offset, PageSize = pageSize },
            transaction: session.Transaction);

        var items = multi.Read<EscrowTransactionDto>().ToList();
        var total = multi.ReadSingle<int>();
        session.Commit();

        foreach (var item in items)
            item.StatusLabel = GetStatusLabel(item.Status);

        return (items, total);
    }

    public EscrowTransactionDto? GetById(int id)
    {
        using var session = new DbSession(_factory, _tenantContext.TenantId);
        var item = session.Connection.QueryFirstOrDefault<EscrowTransactionDto>(
            """
SELECT Id, TenantId, SalesInvoiceId, PartId, BuyerName, BuyerPhone, SellerName, SellerPhone,
       Amount, Currency, Status, Notes, TrackingNumber, ShippedAt, DeliveredAt,
       ReleasedAt, DisputedAt, DisputeReason, AutoReleaseAfterHours, CreatedAt
FROM dbo.EscrowTransactions
WHERE Id = @Id AND (@TenantId = 0 OR TenantId = @TenantId);
""",
            new { Id = id, TenantId = _tenantContext.TenantId },
            transaction: session.Transaction);
        session.Commit();

        if (item is not null)
            item.StatusLabel = GetStatusLabel(item.Status);

        return item;
    }

    public int Create(CreateEscrowRequest request, int createdByUserId)
    {
        if (string.IsNullOrWhiteSpace(request.BuyerName))
            throw new ValidationException("Buyer name is required.");
        if (string.IsNullOrWhiteSpace(request.SellerName))
            throw new ValidationException("Seller name is required.");

        using var session = new DbSession(_factory, _tenantContext.TenantId);
        var id = session.Connection.QuerySingle<int>(
            """
INSERT INTO dbo.EscrowTransactions
    (TenantId, SalesInvoiceId, PartId, BuyerName, BuyerPhone, SellerName, SellerPhone,
     Amount, Currency, Status, Notes, AutoReleaseAfterHours, CreatedAt, CreatedByUserId)
VALUES
    (@TenantId, @SalesInvoiceId, @PartId, @BuyerName, @BuyerPhone, @SellerName, @SellerPhone,
     @Amount, @Currency, @Status, @Notes, @AutoReleaseAfterHours, SYSUTCDATETIME(), @CreatedByUserId);
SELECT SCOPE_IDENTITY();
""",
            new
            {
                TenantId = _tenantContext.TenantId,
                request.SalesInvoiceId,
                request.PartId,
                request.BuyerName,
                request.BuyerPhone,
                request.SellerName,
                request.SellerPhone,
                request.Amount,
                Currency = request.Currency ?? "USD",
                Status = (int)EscrowStatus.PendingPayment,
                request.Notes,
                AutoReleaseAfterHours = request.AutoReleaseAfterHours ?? 48,
                CreatedByUserId = createdByUserId
            },
            transaction: session.Transaction);
        session.Commit();
        return id;
    }

    public void UpdateStatus(int id, UpdateEscrowStatusRequest request, int modifiedByUserId)
    {
        var now = DateTime.UtcNow;
        using var session = new DbSession(_factory, _tenantContext.TenantId);
        session.Connection.Execute(
            """
UPDATE dbo.EscrowTransactions SET
    Status = @Status,
    Notes = ISNULL(@Notes, Notes),
    TrackingNumber = ISNULL(@TrackingNumber, TrackingNumber),
    DisputeReason = ISNULL(@DisputeReason, DisputeReason),
    ShippedAt   = CASE WHEN @Status = 3 THEN @Now ELSE ShippedAt END,
    DeliveredAt = CASE WHEN @Status = 4 THEN @Now ELSE DeliveredAt END,
    ReleasedAt  = CASE WHEN @Status IN (5, 6) THEN @Now ELSE ReleasedAt END,
    DisputedAt  = CASE WHEN @Status = 7 THEN @Now ELSE DisputedAt END,
    ModifiedAt = @Now,
    ModifiedByUserId = @ModifiedByUserId
WHERE Id = @Id AND (@TenantId = 0 OR TenantId = @TenantId);
""",
            new
            {
                Id = id,
                Status = (int)request.NewStatus,
                request.Notes,
                request.TrackingNumber,
                request.DisputeReason,
                Now = now,
                TenantId = _tenantContext.TenantId,
                ModifiedByUserId = modifiedByUserId
            },
            transaction: session.Transaction);
        session.Commit();
    }

    private static string GetStatusLabel(EscrowStatus status) => status switch
    {
        EscrowStatus.PendingPayment => "Pending Payment",
        EscrowStatus.Held => "Held",
        EscrowStatus.Shipped => "Shipped",
        EscrowStatus.Delivered => "Delivered",
        EscrowStatus.BuyerAccepted => "Buyer Accepted",
        EscrowStatus.Released => "Released",
        EscrowStatus.Disputed => "Disputed",
        EscrowStatus.Refunded => "Refunded",
        EscrowStatus.Cancelled => "Cancelled",
        _ => status.ToString()
    };
}
