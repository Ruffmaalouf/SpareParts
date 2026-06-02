using Dapper;
using SpareParts.Domain.Loyalty;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Infrastructure.Services;

public sealed class LoyaltyService
{
    private readonly ISqlConnectionFactory _factory;

    public LoyaltyService(ISqlConnectionFactory factory)
    {
        _factory = factory;
    }

    public CustomerLoyaltyDto? GetCustomerLoyalty(int customerId)
    {
        using var session = new DbSession(_factory);
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
GROUP BY c.Id, c.Name, c.LoyaltyPoints
""",
            new { CustomerId = customerId },
            session.Transaction);
    }

    public IReadOnlyList<CustomerLoyaltyDto> GetTopCustomers(int top = 20)
    {
        using var session = new DbSession(_factory);
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
GROUP BY c.Id, c.Name, c.LoyaltyPoints
ORDER BY c.LoyaltyPoints DESC
""",
            new { Top = top },
            session.Transaction).ToList();
    }

    public IReadOnlyList<LoyaltyTransactionDto> GetTransactions(int customerId)
    {
        using var session = new DbSession(_factory);
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
ORDER BY lt.CreatedAt DESC
""",
            new { CustomerId = customerId },
            session.Transaction).ToList();
    }

    public void AddPoints(AddLoyaltyPointsRequest req, int userId)
    {
        using var session = new DbSession(_factory);
        session.Connection.Execute(
            """
INSERT INTO dbo.LoyaltyTransactions (CustomerId, Points, TransactionType, ReferenceType, ReferenceId, Notes, CreatedAt, CreatedByUserId)
VALUES (@CustomerId, @Points, @TransactionType, @ReferenceType, @ReferenceId, @Notes, SYSUTCDATETIME(), @UserId);

UPDATE dbo.Customers SET LoyaltyPoints = LoyaltyPoints + @Points WHERE Id = @CustomerId;
""",
            new
            {
                req.CustomerId,
                req.Points,
                req.TransactionType,
                req.ReferenceType,
                req.ReferenceId,
                req.Notes,
                UserId = userId
            },
            session.Transaction);
        session.Commit();
    }

    public void RedeemPoints(RedeemLoyaltyPointsRequest req, int userId)
    {
        using var session = new DbSession(_factory);

        var current = session.Connection.ExecuteScalar<int>(
            "SELECT LoyaltyPoints FROM dbo.Customers WHERE Id = @CustomerId",
            new { req.CustomerId },
            session.Transaction);

        if (current < req.Points)
            throw new ConflictException($"Insufficient loyalty points. Customer has {current} but tried to redeem {req.Points}.");

        session.Connection.Execute(
            """
INSERT INTO dbo.LoyaltyTransactions (CustomerId, Points, TransactionType, Notes, CreatedAt, CreatedByUserId)
VALUES (@CustomerId, -@Points, 'Redeem', @Notes, SYSUTCDATETIME(), @UserId);

UPDATE dbo.Customers SET LoyaltyPoints = LoyaltyPoints - @Points WHERE Id = @CustomerId;
""",
            new { req.CustomerId, req.Points, req.Notes, UserId = userId },
            session.Transaction);
        session.Commit();
    }
}
