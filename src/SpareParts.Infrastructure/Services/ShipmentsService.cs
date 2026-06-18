using Dapper;
using SpareParts.Domain.Shipments;
using SpareParts.Infrastructure.Data;
using SpareParts.Infrastructure.Interfaces;

namespace SpareParts.Infrastructure.Services;

public sealed class ShipmentsService
{
    private readonly ISqlConnectionFactory _factory;
    private readonly ITenantContext _tenantContext;

    public ShipmentsService(ISqlConnectionFactory factory, ITenantContext tenantContext)
    {
        _factory = factory;
        _tenantContext = tenantContext;
    }

    public IReadOnlyList<ShipmentDto> GetShipments(string? status = null)
    {
        using var session = new DbSession(_factory, _tenantContext.TenantId);
        return session.Connection.Query<ShipmentDto>(
            """
SELECT TOP 500
    sh.Id,
    sh.ShipmentNumber,
    sh.SalesInvoiceId,
    t.TransactionNumber AS InvoiceNumber,
    sh.CustomerId,
    c.Name AS CustomerName,
    sh.Status,
    sh.CarrierName,
    sh.TrackingNumber,
    sh.DeliveryAddress,
    sh.EstimatedDelivery,
    sh.ActualDelivery,
    sh.Notes,
    sh.CreatedAt
FROM dbo.Shipments sh
LEFT JOIN dbo.Customers c ON c.Id = sh.CustomerId
LEFT JOIN dbo.Transactions t ON t.ReferenceId = sh.SalesInvoiceId
WHERE (@Status IS NULL OR sh.Status = @Status)
  AND (@TenantId = 0 OR sh.TenantId = @TenantId)
ORDER BY sh.CreatedAt DESC
""",
            new { Status = status, session.TenantId },
            session.Transaction).ToList();
    }

    public ShipmentDto? GetShipment(int id)
    {
        using var session = new DbSession(_factory, _tenantContext.TenantId);

        var shipment = session.Connection.QueryFirstOrDefault<ShipmentDto>(
            """
SELECT
    sh.Id, sh.ShipmentNumber, sh.SalesInvoiceId,
    t.TransactionNumber AS InvoiceNumber,
    sh.CustomerId, c.Name AS CustomerName,
    sh.Status, sh.CarrierName, sh.TrackingNumber,
    sh.DeliveryAddress, sh.EstimatedDelivery, sh.ActualDelivery,
    sh.Notes, sh.CreatedAt
FROM dbo.Shipments sh
LEFT JOIN dbo.Customers c ON c.Id = sh.CustomerId
LEFT JOIN dbo.Transactions t ON t.ReferenceId = sh.SalesInvoiceId
WHERE sh.Id = @Id
  AND (@TenantId = 0 OR sh.TenantId = @TenantId)
""",
            new { Id = id, session.TenantId },
            session.Transaction);

        if (shipment == null)
            return null;

        shipment.Events = session.Connection.Query<ShipmentEventDto>(
            """
SELECT Id, ShipmentId, Status, Location, Notes, EventAt
FROM dbo.ShipmentEvents
WHERE ShipmentId = @ShipmentId
ORDER BY EventAt DESC
""",
            new { ShipmentId = id },
            session.Transaction).ToList();

        return shipment;
    }

    public int CreateShipment(CreateShipmentRequest req, int userId)
    {
        using var session = new DbSession(_factory, _tenantContext.TenantId);
        var tenantId = session.TenantId > 0 ? (int?)session.TenantId : null;

        var id = session.Connection.ExecuteScalar<int>(
            """
INSERT INTO dbo.Shipments
    (SalesInvoiceId, CustomerId, Status, CarrierName, TrackingNumber,
     DeliveryAddress, EstimatedDelivery, Notes, CreatedAt, CreatedByUserId, TenantId)
VALUES
    (@SalesInvoiceId, @CustomerId, 'Pending', @CarrierName, @TrackingNumber,
     @DeliveryAddress, @EstimatedDelivery, @Notes, SYSUTCDATETIME(), @UserId, @TenantId);

DECLARE @NewId INT = CAST(SCOPE_IDENTITY() AS INT);
UPDATE dbo.Shipments SET ShipmentNumber = 'SHP-' + CAST(@NewId AS NVARCHAR(20)) WHERE Id = @NewId;

INSERT INTO dbo.ShipmentEvents (ShipmentId, Status, Notes, EventAt, CreatedByUserId)
VALUES (@NewId, 'Pending', 'Shipment created', SYSUTCDATETIME(), @UserId);

SELECT @NewId;
""",
            new
            {
                req.SalesInvoiceId,
                req.CustomerId,
                req.CarrierName,
                req.TrackingNumber,
                req.DeliveryAddress,
                req.EstimatedDelivery,
                req.Notes,
                UserId = userId,
                TenantId = tenantId
            },
            session.Transaction);

        session.Commit();
        return id;
    }

    public void UpdateStatus(int id, UpdateShipmentStatusRequest req, int userId)
    {
        using var session = new DbSession(_factory, _tenantContext.TenantId);

        var affected = session.Connection.Execute(
            """
UPDATE dbo.Shipments
SET Status = @Status,
    ActualDelivery = CASE WHEN @ActualDelivery IS NOT NULL THEN @ActualDelivery ELSE ActualDelivery END,
    ModifiedAt = SYSUTCDATETIME(),
    ModifiedByUserId = @UserId
WHERE Id = @Id
  AND (@TenantId = 0 OR TenantId = @TenantId);

INSERT INTO dbo.ShipmentEvents (ShipmentId, Status, Location, Notes, EventAt, CreatedByUserId)
SELECT @Id, @Status, @Location, @Notes, SYSUTCDATETIME(), @UserId
WHERE EXISTS (SELECT 1 FROM dbo.Shipments WHERE Id = @Id AND (@TenantId = 0 OR TenantId = @TenantId));
""",
            new { Id = id, req.Status, req.Location, req.Notes, req.ActualDelivery, UserId = userId, session.TenantId },
            session.Transaction);

        if (affected == 0)
            throw new NotFoundException("Shipment not found.");

        session.Commit();
    }
}
