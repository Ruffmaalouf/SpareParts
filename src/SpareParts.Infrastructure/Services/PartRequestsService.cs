using Dapper;
using SpareParts.Domain.Inventory;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Infrastructure.Services;

public sealed class PartRequestsService
{
    private readonly ISqlConnectionFactory _factory;

    public PartRequestsService(ISqlConnectionFactory factory)
    {
        _factory = factory;
    }

    public IEnumerable<PartRequestDto> GetAll(string? status = null, string? search = null)
    {
        using var conn = _factory.CreateConnection();
        var normalizedStatus = NormalizeOptional(status);
        var onlyActive = string.Equals(normalizedStatus, "Active", StringComparison.OrdinalIgnoreCase);
        var exactStatus = onlyActive ? null : normalizedStatus;
        var normalizedSearch = NormalizeOptional(search);

        return conn.Query<PartRequestDto>(
            """
SELECT
    pr.Id,
    pr.PartId,
    p.InternalCode AS PartInternalCode,
    p.Name AS MatchedPartName,
    ISNULL(stock.AvailableQuantity, 0) AS AvailableQuantity,
    CASE
        WHEN pr.PartId IS NULL AND pr.Status IN (N'Open', N'Contacted') THEN 1
        ELSE ISNULL(waiting.WaitingCustomerCount, 0)
    END AS WaitingCustomerCount,
    CASE
        WHEN pr.Status IN (N'Open', N'Contacted')
         AND ISNULL(stock.AvailableQuantity, 0) > 0
        THEN CAST(1 AS BIT)
        ELSE CAST(0 AS BIT)
    END AS IsReadyToContact,
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
    SELECT WaitingCustomerCount = COUNT(1)
    FROM
    (
        SELECT
            CustomerKey = COALESCE(
                CONVERT(NVARCHAR(20), peer.CustomerId),
                UPPER(LTRIM(RTRIM(peer.CustomerName))) + N'|' + ISNULL(peer.CustomerPhone, N''))
        FROM dbo.PartRequests peer
        WHERE peer.PartId = pr.PartId
          AND peer.Status IN (N'Open', N'Contacted')
        GROUP BY COALESCE(
            CONVERT(NVARCHAR(20), peer.CustomerId),
            UPPER(LTRIM(RTRIM(peer.CustomerName))) + N'|' + ISNULL(peer.CustomerPhone, N''))
    ) waiters
) waiting
WHERE (@OnlyActive = 0 OR pr.Status IN (N'Open', N'Contacted'))
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
        WHEN pr.Status IN (N'Open', N'Contacted')
         AND ISNULL(stock.AvailableQuantity, 0) > 0
        THEN 0
        ELSE 1
    END,
    CASE WHEN pr.Status IN (N'Open', N'Contacted') THEN 0 ELSE 1 END,
    pr.CreatedAt DESC,
    pr.Id DESC;
""",
            new
            {
                OnlyActive = onlyActive,
                ExactStatus = exactStatus,
                Search = normalizedSearch
            });
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
SELECT Id, InternalCode, Name, OEMNumber
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

    public void UpdateStatus(int id, UpdatePartRequestStatusRequest request, int userId)
    {
        var status = PartRequestStatus.Normalize(request.Status);
        if (!PartRequestStatus.AllStatuses.Contains(status))
        {
            throw new ValidationException("Part request status is invalid.");
        }

        using var conn = _factory.CreateConnection();
        var updated = conn.Execute(
            """
UPDATE dbo.PartRequests
SET Status = @Status,
    ClosedAt = CASE WHEN @Status IN (N'Fulfilled', N'Cancelled') THEN SYSUTCDATETIME() ELSE NULL END,
    ClosedByUserId = CASE WHEN @Status IN (N'Fulfilled', N'Cancelled') THEN @UserId ELSE NULL END,
    ModifiedAt = SYSUTCDATETIME(),
    ModifiedByUserId = @UserId
WHERE Id = @Id;
""",
            new { Id = id, Status = status, UserId = userId });

        if (updated == 0)
        {
            throw new NotFoundException("Part request not found.");
        }
    }

    public void Delete(int id)
    {
        using var conn = _factory.CreateConnection();
        var deleted = conn.Execute("DELETE FROM dbo.PartRequests WHERE Id = @Id;", new { Id = id });
        if (deleted == 0)
        {
            throw new NotFoundException("Part request not found.");
        }
    }

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private sealed class PartLookup
    {
        public int Id { get; set; }
        public string InternalCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? OEMNumber { get; set; }
    }

    private sealed class CustomerLookup
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Phone { get; set; }
    }
}
