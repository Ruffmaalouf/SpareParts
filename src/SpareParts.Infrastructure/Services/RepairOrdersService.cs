using System.ComponentModel.DataAnnotations;
using Dapper;
using SpareParts.Domain.MechanicDesk;
using SpareParts.Infrastructure.Data;
using SpareParts.Infrastructure.Interfaces;

namespace SpareParts.Infrastructure.Services;

public sealed class RepairOrdersService
{
    private readonly ISqlConnectionFactory _factory;
    private readonly ITenantContext _tenantContext;

    public RepairOrdersService(ISqlConnectionFactory factory, ITenantContext tenantContext)
    {
        _factory = factory;
        _tenantContext = tenantContext;
    }

    public (IEnumerable<RepairOrderDto> Items, int TotalCount) GetAll(int page, int pageSize, RepairOrderStatus? status = null)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);
        var offset = (page - 1) * pageSize;

        using var session = new DbSession(_factory, _tenantContext.TenantId);
        using var multi = session.Connection.QueryMultiple(
            """
SELECT ro.Id, ro.TenantId, ro.OrderNumber, ro.CustomerName, ro.CustomerPhone,
       ro.VehicleMake, ro.VehicleModel, ro.VehicleYear, ro.VinNumber,
       ro.Notes, ro.LocationCity, ro.Status, ro.SentAt, ro.DeadlineAt,
       (SELECT COUNT(1) FROM dbo.RepairOrderItems i WHERE i.RepairOrderId = ro.Id) AS ItemCount,
       (SELECT COUNT(1) FROM dbo.RepairOrderQuotes q
            JOIN dbo.RepairOrderItems i2 ON i2.Id = q.RepairOrderItemId
            WHERE i2.RepairOrderId = ro.Id) AS TotalQuotes
FROM dbo.RepairOrders ro
WHERE (@TenantId = 0 OR ro.TenantId = @TenantId)
  AND (@Status IS NULL OR ro.Status = @Status)
ORDER BY ro.CreatedAt DESC
OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;

SELECT COUNT(1) FROM dbo.RepairOrders
WHERE (@TenantId = 0 OR TenantId = @TenantId)
  AND (@Status IS NULL OR Status = @Status);
""",
            new { TenantId = _tenantContext.TenantId, Status = (int?)status, Offset = offset, PageSize = pageSize },
            transaction: session.Transaction);

        var items = multi.Read<RepairOrderDto>().ToList();
        var total = multi.ReadSingle<int>();
        session.Commit();

        foreach (var item in items)
            item.StatusLabel = GetStatusLabel(item.Status);

        return (items, total);
    }

    public RepairOrderDto? GetById(int id)
    {
        using var session = new DbSession(_factory, _tenantContext.TenantId);
        using var multi = session.Connection.QueryMultiple(
            """
SELECT ro.Id, ro.TenantId, ro.OrderNumber, ro.CustomerName, ro.CustomerPhone,
       ro.VehicleMake, ro.VehicleModel, ro.VehicleYear, ro.VinNumber,
       ro.Notes, ro.LocationCity, ro.Status, ro.SentAt, ro.DeadlineAt,
       (SELECT COUNT(1) FROM dbo.RepairOrderItems i WHERE i.RepairOrderId = ro.Id) AS ItemCount,
       (SELECT COUNT(1) FROM dbo.RepairOrderQuotes q
            JOIN dbo.RepairOrderItems i2 ON i2.Id = q.RepairOrderItemId
            WHERE i2.RepairOrderId = ro.Id) AS TotalQuotes
FROM dbo.RepairOrders ro
WHERE ro.Id = @Id AND (@TenantId = 0 OR ro.TenantId = @TenantId);

SELECT i.Id, i.RepairOrderId, i.PartName, i.OemNumber, i.Quantity,
       i.ConditionPreference, i.Notes, i.MaxBudget, i.Currency,
       (SELECT COUNT(1) FROM dbo.RepairOrderQuotes q WHERE q.RepairOrderItemId = i.Id) AS QuoteCount
FROM dbo.RepairOrderItems i
JOIN dbo.RepairOrders ro ON ro.Id = i.RepairOrderId
WHERE i.RepairOrderId = @Id AND (@TenantId = 0 OR ro.TenantId = @TenantId);

SELECT q.Id, q.RepairOrderItemId, q.TenantId, q.SellerName, q.SellerPhone,
       q.PartId, q.OfferedPrice, q.Currency, q.Condition, q.Notes, q.IsAccepted,
       q.ExpiresAt, q.CreatedAt
FROM dbo.RepairOrderQuotes q
JOIN dbo.RepairOrderItems i ON i.Id = q.RepairOrderItemId
WHERE i.RepairOrderId = @Id;
""",
            new { Id = id, TenantId = _tenantContext.TenantId },
            transaction: session.Transaction);

        var order = multi.ReadFirstOrDefault<RepairOrderDto>();
        if (order is null) return null;

        var items = multi.Read<RepairOrderItemDto>().ToList();
        var quotes = multi.Read<RepairOrderQuoteDto>().ToList();
        session.Commit();

        order.StatusLabel = GetStatusLabel(order.Status);
        foreach (var item in items)
            item.Quotes = quotes.Where(q => q.RepairOrderItemId == item.Id).ToList();
        order.Items = items;
        return order;
    }

    public int Create(CreateRepairOrderRequest request, int createdByUserId)
    {
        if (string.IsNullOrWhiteSpace(request.CustomerName))
            throw new ValidationException("Customer name is required.");

        using var session = new DbSession(_factory, _tenantContext.TenantId);
        var orderNumber = $"RO-{_tenantContext.TenantId}-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}";

        var id = session.Connection.QuerySingle<int>(
            """
INSERT INTO dbo.RepairOrders
    (TenantId, OrderNumber, CustomerName, CustomerPhone, VehicleMake, VehicleModel,
     VehicleYear, VinNumber, Notes, LocationCity, Status, DeadlineAt, CreatedAt, CreatedByUserId)
VALUES
    (@TenantId, @OrderNumber, @CustomerName, @CustomerPhone, @VehicleMake, @VehicleModel,
     @VehicleYear, @VinNumber, @Notes, @LocationCity, @Status, @DeadlineAt, SYSUTCDATETIME(), @CreatedByUserId);
SELECT SCOPE_IDENTITY();
""",
            new
            {
                TenantId = _tenantContext.TenantId,
                OrderNumber = orderNumber,
                request.CustomerName,
                request.CustomerPhone,
                request.VehicleMake,
                request.VehicleModel,
                request.VehicleYear,
                request.VinNumber,
                request.Notes,
                request.LocationCity,
                Status = (int)RepairOrderStatus.Draft,
                request.DeadlineAt,
                CreatedByUserId = createdByUserId
            },
            transaction: session.Transaction);

        foreach (var item in request.Items)
        {
            session.Connection.Execute(
                """
INSERT INTO dbo.RepairOrderItems
    (RepairOrderId, PartName, OemNumber, Quantity, ConditionPreference, Notes, MaxBudget, Currency, CreatedAt, CreatedByUserId)
VALUES
    (@RepairOrderId, @PartName, @OemNumber, @Quantity, @ConditionPreference, @Notes, @MaxBudget, @Currency, SYSUTCDATETIME(), @CreatedByUserId);
""",
                new
                {
                    RepairOrderId = id,
                    item.PartName,
                    item.OemNumber,
                    item.Quantity,
                    ConditionPreference = item.ConditionPreference ?? "Any",
                    item.Notes,
                    item.MaxBudget,
                    Currency = item.Currency ?? "USD",
                    CreatedByUserId = createdByUserId
                },
                transaction: session.Transaction);
        }

        session.Commit();
        return id;
    }

    public void Send(int id, int modifiedByUserId)
    {
        using var session = new DbSession(_factory, _tenantContext.TenantId);
        session.Connection.Execute(
            """
UPDATE dbo.RepairOrders
SET Status = @Status, SentAt = SYSUTCDATETIME(), ModifiedAt = SYSUTCDATETIME(), ModifiedByUserId = @ModifiedByUserId
WHERE Id = @Id AND (@TenantId = 0 OR TenantId = @TenantId);
""",
            new { Id = id, Status = (int)RepairOrderStatus.Sent, TenantId = _tenantContext.TenantId, ModifiedByUserId = modifiedByUserId },
            transaction: session.Transaction);
        session.Commit();
    }

    public int AddQuote(CreateRepairOrderQuoteRequest request, int createdByUserId)
    {
        using var session = new DbSession(_factory, _tenantContext.TenantId);
        var id = session.Connection.QuerySingle<int>(
            """
INSERT INTO dbo.RepairOrderQuotes
    (RepairOrderItemId, TenantId, SellerName, SellerPhone, PartId, OfferedPrice, Currency,
     Condition, Notes, IsAccepted, ExpiresAt, CreatedAt, CreatedByUserId)
VALUES
    (@RepairOrderItemId, @TenantId, @SellerName, @SellerPhone, @PartId, @OfferedPrice, @Currency,
     @Condition, @Notes, 0, @ExpiresAt, SYSUTCDATETIME(), @CreatedByUserId);
SELECT SCOPE_IDENTITY();
""",
            new
            {
                request.RepairOrderItemId,
                TenantId = _tenantContext.TenantId,
                request.SellerName,
                request.SellerPhone,
                request.PartId,
                request.OfferedPrice,
                Currency = request.Currency ?? "USD",
                request.Condition,
                request.Notes,
                ExpiresAt = request.ExpiresInHours.HasValue
                    ? DateTime.UtcNow.AddHours(request.ExpiresInHours.Value)
                    : (DateTime?)null,
                CreatedByUserId = createdByUserId
            },
            transaction: session.Transaction);
        session.Commit();
        return id;
    }

    public void AcceptQuote(int quoteId, int modifiedByUserId)
    {
        using var session = new DbSession(_factory, _tenantContext.TenantId);
        session.Connection.Execute(
            "UPDATE dbo.RepairOrderQuotes SET IsAccepted = 1, ModifiedAt = SYSUTCDATETIME(), ModifiedByUserId = @ModifiedByUserId WHERE Id = @Id;",
            new { Id = quoteId, ModifiedByUserId = modifiedByUserId },
            transaction: session.Transaction);
        session.Commit();
    }

    public void Cancel(int id, int modifiedByUserId)
    {
        using var session = new DbSession(_factory, _tenantContext.TenantId);
        session.Connection.Execute(
            """
UPDATE dbo.RepairOrders
SET Status = @Status, ModifiedAt = SYSUTCDATETIME(), ModifiedByUserId = @ModifiedByUserId
WHERE Id = @Id AND (@TenantId = 0 OR TenantId = @TenantId);
""",
            new { Id = id, Status = (int)RepairOrderStatus.Cancelled, TenantId = _tenantContext.TenantId, ModifiedByUserId = modifiedByUserId },
            transaction: session.Transaction);
        session.Commit();
    }

    private static string GetStatusLabel(RepairOrderStatus status) => status switch
    {
        RepairOrderStatus.Draft => "Draft",
        RepairOrderStatus.Sent => "Sent to Sellers",
        RepairOrderStatus.QuotesReceived => "Quotes Received",
        RepairOrderStatus.PartiallyAccepted => "Partially Accepted",
        RepairOrderStatus.Accepted => "Accepted",
        RepairOrderStatus.Cancelled => "Cancelled",
        _ => status.ToString()
    };
}
