using Dapper;
using SpareParts.Domain.BusinessPartners;
using SpareParts.Infrastructure.Data;
using SpareParts.Infrastructure.Interfaces;

namespace SpareParts.Infrastructure.Services;

public sealed class CustomerPriceTierService
{
    private readonly ISqlConnectionFactory _factory;
    private readonly ITenantContext _tenantContext;

    public CustomerPriceTierService(ISqlConnectionFactory factory, ITenantContext tenantContext)
    {
        _factory = factory;
        _tenantContext = tenantContext;
    }

    public IReadOnlyList<CustomerPriceTierDto> GetAll()
    {
        using var session = new DbSession(_factory, _tenantContext.TenantId);
        return session.Connection.Query<CustomerPriceTierDto>(
            """
SELECT
    c.Id AS CustomerId,
    c.Name AS CustomerName,
    c.PriceTier
FROM dbo.Customers c
WHERE (@TenantId = 0 OR c.TenantId = @TenantId)
ORDER BY c.Name
""",
            new { session.TenantId },
            session.Transaction).ToList();
    }

    public void UpdateTier(int customerId, CustomerPriceTier tier, int userId)
    {
        using var session = new DbSession(_factory, _tenantContext.TenantId);
        var affected = session.Connection.Execute(
            """
UPDATE dbo.Customers
SET PriceTier = @Tier,
    ModifiedAt = SYSUTCDATETIME(),
    ModifiedByUserId = @UserId
WHERE Id = @CustomerId AND (@TenantId = 0 OR TenantId = @TenantId)
""",
            new { CustomerId = customerId, Tier = (int)tier, UserId = userId, session.TenantId },
            session.Transaction);

        if (affected == 0)
            throw new NotFoundException("Customer not found.");

        session.Commit();
    }

    public decimal ResolvePartPrice(int partId, int? customerId)
    {
        using var session = new DbSession(_factory, _tenantContext.TenantId);

        var tier = CustomerPriceTier.Retail;
        if (customerId.HasValue)
        {
            var tierValue = session.Connection.ExecuteScalar<int?>(
                "SELECT PriceTier FROM dbo.Customers WHERE Id = @CustomerId AND (@TenantId = 0 OR TenantId = @TenantId)",
                new { CustomerId = customerId.Value, session.TenantId },
                session.Transaction);
            if (tierValue.HasValue)
                tier = (CustomerPriceTier)tierValue.Value;
        }

        var price = session.Connection.ExecuteScalar<decimal>(
            tier switch
            {
                CustomerPriceTier.Wholesale => "SELECT WholesalePrice FROM dbo.Parts WHERE Id = @PartId AND IsActive = 1 AND (@TenantId = 0 OR TenantId = @TenantId)",
                CustomerPriceTier.VIP => "SELECT FastSalePrice FROM dbo.Parts WHERE Id = @PartId AND IsActive = 1 AND (@TenantId = 0 OR TenantId = @TenantId)",
                _ => "SELECT SalePrice FROM dbo.Parts WHERE Id = @PartId AND IsActive = 1 AND (@TenantId = 0 OR TenantId = @TenantId)"
            },
            new { PartId = partId, session.TenantId },
            session.Transaction);

        return price;
    }
}
