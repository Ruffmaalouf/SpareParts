using Dapper;
using SpareParts.Domain.Loyalty;
using SpareParts.Infrastructure.Data;
using SpareParts.Infrastructure.Interfaces;

namespace SpareParts.Infrastructure.Services;

public sealed class LoyaltyService
{
    private readonly ISqlConnectionFactory _factory;
    private readonly ITenantContext _tenantContext;

    public LoyaltyService(ISqlConnectionFactory factory, ITenantContext tenantContext)
    {
        _factory = factory;
        _tenantContext = tenantContext;
    }

    public CustomerLoyaltyDto? GetCustomerLoyalty(int customerId)
    {
        using var session = new DbSession(_factory, _tenantContext.TenantId);
        return session.Connection.QueryFirstOrDefault<CustomerLoyaltyDto>(
            """
SELECT
    c.Id AS CustomerId,
    c.Name AS CustomerName,
    c.LoyaltyPoints AS TotalPoints,
    ISNULL(SUM(CASE WHEN lt.Points > 0 THEN lt.Points ELSE 0 END), 0) AS LifetimePointsEarned,
    ISNULL(SUM(CASE WHEN lt.Points < 0 THEN ABS(lt.Points) ELSE 0 END), 0) AS LifetimePointsRedeemed
FROM dbo.Customers c
LEFT JOIN dbo.LoyaltyTransactions lt ON lt.CustomerId = c.Id
WHERE c.Id = @CustomerId
  AND (@TenantId = 0 OR c.TenantId = @TenantId)
GROUP BY c.Id, c.Name, c.LoyaltyPoints
""",
            new { CustomerId = customerId, session.TenantId },
            session.Transaction);
    }

    public IReadOnlyList<CustomerLoyaltyDto> GetTopCustomers(int top = 20)
    {
        using var session = new DbSession(_factory, _tenantContext.TenantId);
        return session.Connection.Query<CustomerLoyaltyDto>(
            """
SELECT TOP (@Top)
    c.Id AS CustomerId,
    c.Name AS CustomerName,
    c.LoyaltyPoints AS TotalPoints,
    ISNULL(SUM(CASE WHEN lt.Points > 0 THEN lt.Points ELSE 0 END), 0) AS LifetimePointsEarned,
    ISNULL(SUM(CASE WHEN lt.Points < 0 THEN ABS(lt.Points) ELSE 0 END), 0) AS LifetimePointsRedeemed
FROM dbo.Customers c
LEFT JOIN dbo.LoyaltyTransactions lt ON lt.CustomerId = c.Id
WHERE (@TenantId = 0 OR c.TenantId = @TenantId)
GROUP BY c.Id, c.Name, c.LoyaltyPoints
ORDER BY c.LoyaltyPoints DESC
""",
            new { Top = top, session.TenantId },
            session.Transaction).ToList();
    }

    public IReadOnlyList<LoyaltyTransactionDto> GetTransactions(int customerId)
    {
        using var session = new DbSession(_factory, _tenantContext.TenantId);
        return session.Connection.Query<LoyaltyTransactionDto>(
            """
SELECT TOP 200
    lt.Id,
    lt.CustomerId,
    c.Name AS CustomerName,
    lt.Points,
    lt.TransactionType,
    lt.ReferenceType,
    lt.ReferenceId,
    lt.Notes,
    lt.CreatedAt
FROM dbo.LoyaltyTransactions lt
INNER JOIN dbo.Customers c ON c.Id = lt.CustomerId
WHERE lt.CustomerId = @CustomerId
  AND (@TenantId = 0 OR c.TenantId = @TenantId)
ORDER BY lt.CreatedAt DESC
""",
            new { CustomerId = customerId, session.TenantId },
            session.Transaction).ToList();
    }

    public void AddPoints(AddLoyaltyPointsRequest req, int userId)
    {
        using var session = new DbSession(_factory, _tenantContext.TenantId);
        var tenantId = session.TenantId > 0 ? (int?)session.TenantId : null;

        var affected = session.Connection.Execute(
            """
INSERT INTO dbo.LoyaltyTransactions (CustomerId, Points, TransactionType, ReferenceType, ReferenceId, Notes, CreatedAt, CreatedByUserId, TenantId)
SELECT @CustomerId, @Points, @TransactionType, @ReferenceType, @ReferenceId, @Notes, SYSUTCDATETIME(), @UserId, @TenantId
WHERE EXISTS (SELECT 1 FROM dbo.Customers WHERE Id = @CustomerId AND (@SessionTenantId = 0 OR TenantId = @SessionTenantId));

UPDATE dbo.Customers SET LoyaltyPoints = LoyaltyPoints + @Points
WHERE Id = @CustomerId AND (@SessionTenantId = 0 OR TenantId = @SessionTenantId);
""",
            new
            {
                req.CustomerId,
                req.Points,
                req.TransactionType,
                req.ReferenceType,
                req.ReferenceId,
                req.Notes,
                UserId = userId,
                TenantId = tenantId,
                SessionTenantId = session.TenantId
            },
            session.Transaction);

        if (affected == 0)
            throw new NotFoundException("Customer not found.");

        session.Commit();
    }

    public void RedeemPoints(RedeemLoyaltyPointsRequest req, int userId)
    {
        using var session = new DbSession(_factory, _tenantContext.TenantId);

        var current = session.Connection.ExecuteScalar<int?>(
            "SELECT LoyaltyPoints FROM dbo.Customers WHERE Id = @CustomerId AND (@TenantId = 0 OR TenantId = @TenantId)",
            new { req.CustomerId, session.TenantId },
            session.Transaction);

        if (current == null)
            throw new NotFoundException("Customer not found.");

        if (current < req.Points)
            throw new ConflictException($"Insufficient loyalty points. Customer has {current} but tried to redeem {req.Points}.");

        var tenantId = session.TenantId > 0 ? (int?)session.TenantId : null;

        session.Connection.Execute(
            """
INSERT INTO dbo.LoyaltyTransactions (CustomerId, Points, TransactionType, Notes, CreatedAt, CreatedByUserId, TenantId)
VALUES (@CustomerId, -@Points, 'Redeem', @Notes, SYSUTCDATETIME(), @UserId, @TenantId);

UPDATE dbo.Customers SET LoyaltyPoints = LoyaltyPoints - @Points
WHERE Id = @CustomerId AND (@SessionTenantId = 0 OR TenantId = @SessionTenantId);
""",
            new { req.CustomerId, req.Points, req.Notes, UserId = userId, TenantId = tenantId, SessionTenantId = session.TenantId },
            session.Transaction);
        session.Commit();
    }
}
