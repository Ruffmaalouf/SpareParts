using Dapper;
using SpareParts.Domain.BusinessPartners;
using SpareParts.Infrastructure.Data;

namespace SpareParts.Infrastructure.Services;

public sealed class CustomerPriceTierService
{
    private readonly ISqlConnectionFactory _factory;

    public CustomerPriceTierService(ISqlConnectionFactory factory)
    {
        _factory = factory;
    }

    public IReadOnlyList<CustomerPriceTierDto> GetAll()
    {
        using var session = new DbSession(_factory);
        return session.Connection.Query<CustomerPriceTierDto>(
            """
SELECT
    c.Id AS CustomerId,
    c.Name AS CustomerName,
    c.PriceTier
FROM dbo.Customers c
ORDER BY c.Name
""",
            transaction: session.Transaction).ToList();
    }

    public void UpdateTier(int customerId, CustomerPriceTier tier, int userId)
    {
        using var session = new DbSession(_factory);
        var affected = session.Connection.Execute(
            """
UPDATE dbo.Customers
SET PriceTier = @Tier,
    ModifiedAt = SYSUTCDATETIME(),
    ModifiedByUserId = @UserId
WHERE Id = @CustomerId
""",
            new { CustomerId = customerId, Tier = (int)tier, UserId = userId },
            session.Transaction);

        if (affected == 0)
            throw new NotFoundException("Customer not found.");

        session.Commit();
    }

    public decimal ResolvePartPrice(int partId, int? customerId)
    {
        using var session = new DbSession(_factory);

        var tier = CustomerPriceTier.Retail;
        if (customerId.HasValue)
        {
            var tierValue = session.Connection.ExecuteScalar<int?>(
                "SELECT PriceTier FROM dbo.Customers WHERE Id = @CustomerId",
                new { CustomerId = customerId.Value },
                session.Transaction);
            if (tierValue.HasValue)
                tier = (CustomerPriceTier)tierValue.Value;
        }

        var price = session.Connection.ExecuteScalar<decimal>(
            tier switch
            {
                CustomerPriceTier.Wholesale => "SELECT WholesalePrice FROM dbo.Parts WHERE Id = @PartId AND IsActive = 1",
                CustomerPriceTier.VIP => "SELECT FastSalePrice FROM dbo.Parts WHERE Id = @PartId AND IsActive = 1",
                _ => "SELECT SalePrice FROM dbo.Parts WHERE Id = @PartId AND IsActive = 1"
            },
            new { PartId = partId },
            session.Transaction);

        return price;
    }
}
