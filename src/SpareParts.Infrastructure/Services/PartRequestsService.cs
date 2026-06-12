using System.Data;
using Dapper;
using SpareParts.Domain.Inventory;
using SpareParts.Domain.Pricing;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Infrastructure.Services;

public sealed class PartRequestsService
{
    private const string ReservationStatusActive = "Active";
    private const string ReservationStatusReleased = "Released";
    private const string ReservationStatusFulfilled = "Fulfilled";
    private const string ReleaseReasonExpired = "Reservation expired";
    private const string ReleaseReasonReplaced = "Reservation replaced";
    private const string ReleaseReasonManual = "Released by staff";

    private readonly ISqlConnectionFactory _factory;
    private readonly ISubscriptionLimitService _subscriptionLimitService;

    public PartRequestsService(ISqlConnectionFactory factory, ISubscriptionLimitService subscriptionLimitService)
    {
        _factory = factory;
        _subscriptionLimitService = subscriptionLimitService;
    }

    public IEnumerable<PartRequestDto> GetAll(string? status = null, string? search = null)
    {
        using var conn = _factory.CreateConnection();
        var normalizedStatus = NormalizeOptional(status);
        var onlyActive = string.Equals(normalizedStatus, "Active", StringComparison.OrdinalIgnoreCase);
        var exactStatus = onlyActive ? null : normalizedStatus;
        var normalizedSearch = NormalizeOptional(search);

        var rows = conn.Query<PartRequestDto>(
            """
SELECT
    pr.Id,
    pr.PartId,
    p.InternalCode AS PartInternalCode,
    p.Name AS MatchedPartName,
    ISNULL(stock.AvailableQuantity, 0) AS AvailableQuantity,
    CASE
        WHEN pr.PartId IS NULL AND pr.Status IN (N'Open', N'Contacted', N'Reserved') THEN 1
        ELSE ISNULL(waiting.WaitingCustomerCount, 0)
    END AS WaitingCustomerCount,
    CASE
        WHEN pr.Status IN (N'Open', N'Contacted')
         AND ISNULL(stock.AvailableQuantity, 0) > 0
        THEN CAST(1 AS BIT)
        ELSE CAST(0 AS BIT)
    END AS IsReadyToContact,
    ISNULL(reservation.ReservedQuantity, 0) AS ReservedQuantity,
    reservation.ReservationExpiresAt,
    reservation.ReservationExpirationAction,
    reservation.ReservationReminderSentAt,
    CASE WHEN ISNULL(reservation.ReservedQuantity, 0) > 0 THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS IsReserved,
    CASE
        WHEN ISNULL(reservation.ReservedQuantity, 0) > 0
         AND reservation.ReservationExpiresAt <= SYSUTCDATETIME()
        THEN CAST(1 AS BIT)
        ELSE CAST(0 AS BIT)
    END AS IsReservationOverdue,
    CASE
        WHEN ISNULL(reservation.ReservedQuantity, 0) > 0
         AND reservation.ReservationExpirationAction = N'StaffReminder'
         AND reservation.ReservationExpiresAt <= SYSUTCDATETIME()
        THEN CAST(1 AS BIT)
        ELSE CAST(0 AS BIT)
    END AS IsReservationReminderDue,
    pr.CustomerId,
    pr.CustomerName,
    pr.CustomerPhone,
    pr.RequestedPartName,
    pr.RequestedOemNumber,
    pr.VehicleDetails,
    pr.Quantity,
    pr.Status,
    pr.Notes,
    pr.CreatedAt,
    pr.ClosedAt
FROM dbo.PartRequests pr
LEFT JOIN dbo.Parts p ON p.Id = pr.PartId
OUTER APPLY
(
    SELECT AvailableQuantity = ISNULL(SUM(s.Quantity - ISNULL(s.ReservedQuantity, 0)), 0)
    FROM dbo.Stock s
    WHERE s.PartId = pr.PartId
) stock
OUTER APPLY
(
    SELECT
        ReservedQuantity = ISNULL(SUM(r.Quantity), 0),
        ReservationExpiresAt = MIN(r.ExpiresAt),
        ReservationExpirationAction = CASE
            WHEN MAX(CASE WHEN r.ExpirationAction = N'StaffReminder' THEN 1 ELSE 0 END) = 1
            THEN N'StaffReminder'
            ELSE MAX(r.ExpirationAction)
        END,
        ReservationReminderSentAt = MIN(r.ReminderSentAt)
    FROM dbo.PartRequestReservations r
    WHERE r.PartRequestId = pr.Id
      AND r.Status = N'Active'
) reservation
OUTER APPLY
(
    SELECT WaitingCustomerCount = COUNT(1)
    FROM
    (
        SELECT
            CustomerKey = COALESCE(
                CONVERT(NVARCHAR(20), peer.CustomerId),
                UPPER(LTRIM(RTRIM(peer.CustomerName))) + N'|' + ISNULL(peer.CustomerPhone, N''))
        FROM dbo.PartRequests peer
        WHERE peer.PartId = pr.PartId
          AND peer.Status IN (N'Open', N'Contacted', N'Reserved')
        GROUP BY COALESCE(
            CONVERT(NVARCHAR(20), peer.CustomerId),
            UPPER(LTRIM(RTRIM(peer.CustomerName))) + N'|' + ISNULL(peer.CustomerPhone, N''))
    ) waiters
) waiting
WHERE (@OnlyActive = 0 OR pr.Status IN (N'Open', N'Contacted', N'Reserved'))
  AND (@ExactStatus IS NULL OR pr.Status = @ExactStatus)
  AND
  (
      @Search IS NULL
      OR pr.CustomerName LIKE N'%' + @Search + N'%'
      OR ISNULL(pr.CustomerPhone, N'') LIKE N'%' + @Search + N'%'
      OR pr.RequestedPartName LIKE N'%' + @Search + N'%'
      OR ISNULL(pr.RequestedOemNumber, N'') LIKE N'%' + @Search + N'%'
      OR ISNULL(pr.VehicleDetails, N'') LIKE N'%' + @Search + N'%'
      OR ISNULL(p.InternalCode, N'') LIKE N'%' + @Search + N'%'
      OR ISNULL(p.Name, N'') LIKE N'%' + @Search + N'%'
  )
ORDER BY
    CASE
        WHEN reservation.ReservationExpirationAction = N'StaffReminder'
         AND reservation.ReservationExpiresAt <= SYSUTCDATETIME() THEN 0
        WHEN pr.Status IN (N'Open', N'Contacted')
         AND ISNULL(stock.AvailableQuantity, 0) > 0 THEN 1
        WHEN ISNULL(reservation.ReservedQuantity, 0) > 0 THEN 2
        ELSE 3
    END,
    CASE WHEN pr.Status IN (N'Open', N'Contacted', N'Reserved') THEN 0 ELSE 1 END,
    pr.CreatedAt DESC,
    pr.Id DESC;
""",
            new
            {
                OnlyActive = onlyActive,
                ExactStatus = exactStatus,
                Search = normalizedSearch
            }).ToList();

        foreach (var row in rows)
        {
            row.ReservationExpiresAt = SpecifyUtc(row.ReservationExpiresAt);
            row.ReservationReminderSentAt = SpecifyUtc(row.ReservationReminderSentAt);
        }

        return rows;
    }

    public int Create(CreatePartRequestItemRequest request, int userId)
    {
        if (request.Quantity <= 0)
        {
            throw new ValidationException("Quantity must be greater than zero.");
        }

        using var conn = _factory.CreateConnection();
        var part = request.PartId is int partId
            ? conn.QuerySingleOrDefault<PartLookup>(
                """
SELECT Id, InternalCode, Name, OEMNumber, TenantId
FROM dbo.Parts
WHERE Id = @PartId
  AND IsActive = 1;
""",
                new { PartId = partId })
            : null;

        if (request.PartId is int && part == null)
        {
            throw new NotFoundException("Part not found.");
        }

        var customer = request.CustomerId is int customerId
            ? conn.QuerySingleOrDefault<CustomerLookup>(
                """
SELECT Id, Name, Phone
FROM dbo.Customers
WHERE Id = @CustomerId;
""",
                new { CustomerId = customerId })
            : null;

        if (request.CustomerId is int && customer == null)
        {
            throw new NotFoundException("Customer not found.");
        }

        var customerName = NormalizeOptional(request.CustomerName) ?? customer?.Name;
        if (string.IsNullOrWhiteSpace(customerName))
        {
            throw new ValidationException("Customer name is required.");
        }

        var requestedPartName = NormalizeOptional(request.RequestedPartName) ?? part?.Name;
        if (string.IsNullOrWhiteSpace(requestedPartName))
        {
            throw new ValidationException("Requested part name is required.");
        }

        if (part?.TenantId is int sellerTenantId)
        {
            _subscriptionLimitService.EnsureAndIncrementMonthlyUsage(sellerTenantId, LimitCode.BuyerLeadsMonthly, "Monthly Buyer Leads");
        }

        return conn.ExecuteScalar<int>(
            """
INSERT INTO dbo.PartRequests
    (PartId, CustomerId, CustomerName, CustomerPhone, RequestedPartName, RequestedOemNumber, VehicleDetails, Quantity, Status, Notes, CreatedByUserId)
VALUES
    (@PartId, @CustomerId, @CustomerName, @CustomerPhone, @RequestedPartName, @RequestedOemNumber, @VehicleDetails, @Quantity, N'Open', @Notes, @UserId);
SELECT CAST(SCOPE_IDENTITY() AS INT);
""",
            new
            {
                request.PartId,
                request.CustomerId,
                CustomerName = customerName.Trim(),
                CustomerPhone = NormalizeOptional(request.CustomerPhone) ?? customer?.Phone,
                RequestedPartName = requestedPartName.Trim(),
                RequestedOemNumber = NormalizeOptional(request.RequestedOemNumber) ?? part?.OEMNumber,
                VehicleDetails = NormalizeOptional(request.VehicleDetails),
                request.Quantity,
                Notes = NormalizeOptional(request.Notes),
                UserId = userId
            });
    }

    public PartRequestReservationDto Reserve(int id, ReservePartRequestRequest request, int userId)
    {
        if (request == null)
        {
            throw new ValidationException("Reservation request is required.");
        }

        var expirationAction = PartReservationExpirationAction.Normalize(request.ExpirationAction);
        if (!PartReservationExpirationAction.AllActions.Contains(expirationAction))
        {
            throw new ValidationException("Reservation deadline action is invalid.");
        }

        var expiresAt = PartReservationClock.ResolveExpiresAtUtc(request.ExpiresAt, DateTimeOffset.Now);
        if (expiresAt <= DateTime.UtcNow)
        {
            throw new ValidationException("Reservation deadline must be in the future.");
        }

        using var conn = _factory.CreateConnection();
        using var tx = conn.BeginTransaction();

        try
        {
            var target = conn.QuerySingleOrDefault<ReservationTarget>(
                """
SELECT Id, PartId, Quantity, Status
FROM dbo.PartRequests WITH (UPDLOCK, ROWLOCK)
WHERE Id = @Id;
""",
                new { Id = id },
                tx);

            if (target == null)
            {
                throw new NotFoundException("Part request not found.");
            }

            if (target.PartId == null)
            {
                throw new ValidationException("Match a catalog part before starting a reservation clock.");
            }

            if (target.Status is PartRequestStatus.Fulfilled or PartRequestStatus.Cancelled)
            {
                throw new ValidationException("Closed part requests cannot be reserved.");
            }

            var quantity = request.Quantity.GetValueOrDefault(target.Quantity);
            if (quantity <= 0)
            {
                throw new ValidationException("Reservation quantity must be greater than zero.");
            }

            ReleaseActiveReservations(conn, tx, id, userId, ReservationStatusReleased, ReleaseReasonReplaced);
            AllocateReservation(conn, tx, id, target.PartId.Value, request.WarehouseId, quantity, expiresAt, expirationAction, userId);

            conn.Execute(
                """
UPDATE dbo.PartRequests
SET Status = N'Reserved',
    ClosedAt = NULL,
    ClosedByUserId = NULL,
    ModifiedAt = SYSUTCDATETIME(),
    ModifiedByUserId = @UserId
WHERE Id = @Id;
""",
                new { Id = id, UserId = userId },
                tx);

            tx.Commit();

            return new PartRequestReservationDto
            {
                PartRequestId = id,
                Quantity = quantity,
                ExpiresAt = expiresAt,
                ExpirationAction = expirationAction
            };
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    public void ReleaseReservation(int id, ReleasePartRequestReservationRequest request, int userId)
    {
        var nextStatus = PartRequestStatus.Normalize(request?.NextStatus ?? PartRequestStatus.Contacted);
        if (!PartRequestStatus.AllStatuses.Contains(nextStatus) || nextStatus == PartRequestStatus.Reserved)
        {
            throw new ValidationException("Reservation release status is invalid.");
        }

        using var conn = _factory.CreateConnection();
        using var tx = conn.BeginTransaction();

        try
        {
            EnsurePartRequestExists(conn, tx, id);
            ReleaseActiveReservations(
                conn,
                tx,
                id,
                userId,
                ReservationStatusReleased,
                NormalizeOptional(request?.Reason) ?? ReleaseReasonManual);
            UpdatePartRequestStatus(conn, tx, id, nextStatus, userId);
            tx.Commit();
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    public void UpdateStatus(int id, UpdatePartRequestStatusRequest request, int userId)
    {
        var status = PartRequestStatus.Normalize(request.Status);
        if (!PartRequestStatus.AllStatuses.Contains(status))
        {
            throw new ValidationException("Part request status is invalid.");
        }

        using var conn = _factory.CreateConnection();
        using var tx = conn.BeginTransaction();

        try
        {
            EnsurePartRequestExists(conn, tx, id);

            if (status == PartRequestStatus.Reserved)
            {
                var activeReservationCount = conn.ExecuteScalar<int>(
                    """
SELECT COUNT(1)
FROM dbo.PartRequestReservations
WHERE PartRequestId = @Id
  AND Status = N'Active';
""",
                    new { Id = id },
                    tx);

                if (activeReservationCount == 0)
                {
                    throw new ValidationException("Use the reservation action to start a reservation clock.");
                }
            }
            else
            {
                var reservationStatus = status == PartRequestStatus.Fulfilled
                    ? ReservationStatusFulfilled
                    : ReservationStatusReleased;
                ReleaseActiveReservations(conn, tx, id, userId, reservationStatus, $"Marked {status}");
            }

            UpdatePartRequestStatus(conn, tx, id, status, userId);
            tx.Commit();
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    public void Delete(int id)
    {
        using var conn = _factory.CreateConnection();
        using var tx = conn.BeginTransaction();

        try
        {
            ReleaseActiveReservations(conn, tx, id, null, ReservationStatusReleased, "Part request deleted");
            var deleted = conn.Execute("DELETE FROM dbo.PartRequests WHERE Id = @Id;", new { Id = id }, tx);
            if (deleted == 0)
            {
                throw new NotFoundException("Part request not found.");
            }

            tx.Commit();
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    public PartReservationClockRunResult ProcessReservationDeadlines(DateTime utcNow)
    {
        using var conn = _factory.CreateConnection();
        var reminders = MarkStaffReminders(conn, utcNow);
        var releasedCount = ReleaseExpiredAutoReservations(conn, utcNow);

        return new PartReservationClockRunResult
        {
            ReleasedReservationCount = releasedCount,
            Reminders = reminders
        };
    }

    private static void AllocateReservation(
        IDbConnection conn,
        IDbTransaction tx,
        int partRequestId,
        int partId,
        int? warehouseId,
        int quantity,
        DateTime expiresAt,
        string expirationAction,
        int userId)
    {
        var stockRows = conn.Query<ReservationStockRow>(
            """
SELECT
    s.Id,
    s.WarehouseId,
    AvailableQuantity = s.Quantity - ISNULL(s.ReservedQuantity, 0)
FROM dbo.Stock s WITH (UPDLOCK, ROWLOCK)
LEFT JOIN dbo.Warehouses w ON w.Id = s.WarehouseId
WHERE s.PartId = @PartId
  AND (@WarehouseId IS NULL OR s.WarehouseId = @WarehouseId)
  AND s.Quantity - ISNULL(s.ReservedQuantity, 0) > 0
ORDER BY CASE WHEN ISNULL(w.IsMain, 0) = 1 THEN 0 ELSE 1 END, s.Id;
""",
            new { PartId = partId, WarehouseId = warehouseId },
            tx).ToList();

        var remaining = quantity;
        var allocated = 0;
        foreach (var row in stockRows)
        {
            if (remaining <= 0)
            {
                break;
            }

            var reservedQuantity = Math.Min(row.AvailableQuantity, remaining);
            var updated = conn.Execute(
                """
UPDATE dbo.Stock
SET ReservedQuantity = ISNULL(ReservedQuantity, 0) + @Quantity,
    ModifiedAt = SYSUTCDATETIME(),
    ModifiedByUserId = @UserId
WHERE Id = @StockId
  AND Quantity - ISNULL(ReservedQuantity, 0) >= @Quantity;
""",
                new
                {
                    StockId = row.Id,
                    Quantity = reservedQuantity,
                    UserId = userId
                },
                tx);

            if (updated == 0)
            {
                throw new ConflictException("Stock changed while creating the reservation. Try again.");
            }

            conn.Execute(
                """
INSERT INTO dbo.PartRequestReservations
    (PartRequestId, StockId, Quantity, ExpiresAt, ExpirationAction, Status, CreatedByUserId)
VALUES
    (@PartRequestId, @StockId, @Quantity, @ExpiresAt, @ExpirationAction, N'Active', @UserId);
""",
                new
                {
                    PartRequestId = partRequestId,
                    StockId = row.Id,
                    Quantity = reservedQuantity,
                    ExpiresAt = expiresAt,
                    ExpirationAction = expirationAction,
                    UserId = userId
                },
                tx);

            remaining -= reservedQuantity;
            allocated += reservedQuantity;
        }

        if (remaining > 0)
        {
            throw new ConflictException($"Not enough available stock to reserve {quantity:N0} item(s). Available: {allocated:N0}.");
        }
    }

    private static int ReleaseExpiredAutoReservations(IDbConnection conn, DateTime utcNow)
    {
        var requestIds = conn.Query<int>(
            """
SELECT DISTINCT PartRequestId
FROM dbo.PartRequestReservations
WHERE Status = N'Active'
  AND ExpirationAction = N'AutoRelease'
  AND ExpiresAt <= @UtcNow;
""",
            new { UtcNow = utcNow }).ToList();

        var releasedCount = 0;
        foreach (var requestId in requestIds)
        {
            using var tx = conn.BeginTransaction();
            try
            {
                releasedCount += ReleaseActiveReservations(
                    conn,
                    tx,
                    requestId,
                    null,
                    ReservationStatusReleased,
                    ReleaseReasonExpired);
                conn.Execute(
                    """
UPDATE dbo.PartRequests
SET Status = CASE WHEN Status = N'Reserved' THEN N'Contacted' ELSE Status END,
    ModifiedAt = SYSUTCDATETIME()
WHERE Id = @Id;
""",
                    new { Id = requestId },
                    tx);
                tx.Commit();
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        return releasedCount;
    }

    private static IReadOnlyList<PartReservationReminderDto> MarkStaffReminders(IDbConnection conn, DateTime utcNow)
    {
        var reminders = conn.Query<PartReservationReminderDto>(
            """
SELECT
    PartRequestId = pr.Id,
    pr.CustomerName,
    pr.RequestedPartName,
    ReservedQuantity = SUM(r.Quantity),
    ExpiresAt = MIN(r.ExpiresAt)
FROM dbo.PartRequestReservations r
INNER JOIN dbo.PartRequests pr ON pr.Id = r.PartRequestId
WHERE r.Status = N'Active'
  AND r.ExpirationAction = N'StaffReminder'
  AND r.ExpiresAt <= @UtcNow
  AND r.ReminderSentAt IS NULL
GROUP BY pr.Id, pr.CustomerName, pr.RequestedPartName;
""",
            new { UtcNow = utcNow }).ToList();

        if (reminders.Count > 0)
        {
            conn.Execute(
                """
UPDATE dbo.PartRequestReservations
SET ReminderSentAt = @UtcNow
WHERE Status = N'Active'
  AND ExpirationAction = N'StaffReminder'
  AND ExpiresAt <= @UtcNow
  AND ReminderSentAt IS NULL;
""",
                new { UtcNow = utcNow });
        }

        foreach (var reminder in reminders)
        {
            reminder.ExpiresAt = SpecifyUtc(reminder.ExpiresAt) ?? reminder.ExpiresAt;
        }

        return reminders;
    }

    private static int ReleaseActiveReservations(
        IDbConnection conn,
        IDbTransaction tx,
        int partRequestId,
        int? userId,
        string reservationStatus,
        string reason)
    {
        var lines = conn.Query<ActiveReservationLine>(
            """
SELECT Id, StockId, Quantity
FROM dbo.PartRequestReservations WITH (UPDLOCK, ROWLOCK)
WHERE PartRequestId = @PartRequestId
  AND Status = N'Active';
""",
            new { PartRequestId = partRequestId },
            tx).ToList();

        foreach (var line in lines)
        {
            conn.Execute(
                """
UPDATE dbo.Stock
SET ReservedQuantity = CASE
        WHEN ISNULL(ReservedQuantity, 0) >= @Quantity THEN ISNULL(ReservedQuantity, 0) - @Quantity
        ELSE 0
    END,
    ModifiedAt = SYSUTCDATETIME(),
    ModifiedByUserId = @UserId
WHERE Id = @StockId;
""",
                new
                {
                    StockId = line.StockId,
                    line.Quantity,
                    UserId = userId
                },
                tx);

            conn.Execute(
                """
UPDATE dbo.PartRequestReservations
SET Status = @Status,
    ReleasedAt = SYSUTCDATETIME(),
    ReleasedByUserId = @UserId,
    ReleaseReason = @Reason
WHERE Id = @Id;
""",
                new
                {
                    line.Id,
                    Status = reservationStatus,
                    UserId = userId,
                    Reason = reason
                },
                tx);
        }

        return lines.Sum(line => line.Quantity);
    }

    private static void EnsurePartRequestExists(IDbConnection conn, IDbTransaction tx, int id)
    {
        var exists = conn.ExecuteScalar<int>(
            """
SELECT COUNT(1)
FROM dbo.PartRequests WITH (UPDLOCK, ROWLOCK)
WHERE Id = @Id;
""",
            new { Id = id },
            tx);

        if (exists == 0)
        {
            throw new NotFoundException("Part request not found.");
        }
    }

    private static void UpdatePartRequestStatus(IDbConnection conn, IDbTransaction tx, int id, string status, int userId)
    {
        conn.Execute(
            """
UPDATE dbo.PartRequests
SET Status = @Status,
    ClosedAt = CASE WHEN @Status IN (N'Fulfilled', N'Cancelled') THEN SYSUTCDATETIME() ELSE NULL END,
    ClosedByUserId = CASE WHEN @Status IN (N'Fulfilled', N'Cancelled') THEN @UserId ELSE NULL END,
    ModifiedAt = SYSUTCDATETIME(),
    ModifiedByUserId = @UserId
WHERE Id = @Id;
""",
            new { Id = id, Status = status, UserId = userId },
            tx);
    }

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static DateTime? SpecifyUtc(DateTime? value)
        => value.HasValue && value.Value.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc)
            : value;

    private sealed class PartLookup
    {
        public int Id { get; set; }
        public string InternalCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? OEMNumber { get; set; }
        public int? TenantId { get; set; }
    }

    private sealed class CustomerLookup
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Phone { get; set; }
    }

    private sealed class ReservationTarget
    {
        public int Id { get; set; }
        public int? PartId { get; set; }
        public int Quantity { get; set; }
        public string Status { get; set; } = PartRequestStatus.Open;
    }

    private sealed class ReservationStockRow
    {
        public int Id { get; set; }
        public int WarehouseId { get; set; }
        public int AvailableQuantity { get; set; }
    }

    private sealed class ActiveReservationLine
    {
        public int Id { get; set; }
        public int StockId { get; set; }
        public int Quantity { get; set; }
    }
}
