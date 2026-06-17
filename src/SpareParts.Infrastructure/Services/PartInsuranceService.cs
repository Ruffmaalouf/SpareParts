using System.ComponentModel.DataAnnotations;
using Dapper;
using SpareParts.Domain.Insurance;
using SpareParts.Infrastructure.Data;
using SpareParts.Infrastructure.Interfaces;

namespace SpareParts.Infrastructure.Services;

public sealed class PartInsuranceService
{
    private readonly ISqlConnectionFactory _factory;
    private readonly ITenantContext _tenantContext;

    public PartInsuranceService(ISqlConnectionFactory factory, ITenantContext tenantContext)
    {
        _factory = factory;
        _tenantContext = tenantContext;
    }

    public IEnumerable<InsuranceOptionDto> GetOptions()
    {
        using var connection = _factory.CreateConnection();

        return connection.Query<InsuranceOptionDto>(
            "SELECT Id, Name, Description, PricePercent, MaxCoverageAmount, Currency FROM dbo.InsuranceOptions WHERE IsActive = 1 ORDER BY PricePercent ASC;");
    }

    public InsuranceAddonDto AddAddon(AddInsuranceAddonRequest request, int createdByUserId)
    {
        if (request.InsuranceOptionId <= 0)
            throw new ValidationException("A valid insurance option must be specified.");
        if (!request.PartId.HasValue && !request.SalesInvoiceId.HasValue)
            throw new ValidationException("Either a part or a sales invoice must be specified.");

        using var session = new DbSession(_factory, _tenantContext.TenantId);

        var option = session.Connection.QuerySingleOrDefault<InsuranceOption>(
            "SELECT Id, Name, Description, PricePercent, MaxCoverageAmount, Currency, IsActive FROM dbo.InsuranceOptions WHERE Id = @Id;",
            new { Id = request.InsuranceOptionId },
            session.Transaction);

        if (option is null)
            throw new NotFoundException($"Insurance option {request.InsuranceOptionId} not found.");

        decimal baseAmount = 0m;

        if (request.PartId.HasValue)
        {
            baseAmount = session.Connection.ExecuteScalar<decimal>(
                "SELECT ISNULL(SalePrice, 0) FROM dbo.Parts WHERE Id = @PartId AND TenantId = @TenantId;",
                new { PartId = request.PartId.Value, TenantId = _tenantContext.TenantId },
                session.Transaction);
        }

        var price = Math.Round(baseAmount * (option.PricePercent / 100m), 2);
        var now = DateTime.UtcNow;

        var id = session.Connection.ExecuteScalar<int>(
            """
INSERT INTO dbo.InsuranceAddons (TenantId, SalesInvoiceId, PartId, InsuranceOptionId, Price, Currency, CreatedAt, CreatedByUserId)
VALUES (@TenantId, @SalesInvoiceId, @PartId, @InsuranceOptionId, @Price, @Currency, @CreatedAt, @CreatedByUserId);
SELECT SCOPE_IDENTITY();
""",
            new
            {
                TenantId = _tenantContext.TenantId,
                SalesInvoiceId = request.SalesInvoiceId,
                PartId = request.PartId,
                InsuranceOptionId = request.InsuranceOptionId,
                Price = price,
                Currency = option.Currency,
                CreatedAt = now,
                CreatedByUserId = createdByUserId
            },
            session.Transaction);

        session.Commit();

        return new InsuranceAddonDto
        {
            Id = id,
            TenantId = _tenantContext.TenantId,
            SalesInvoiceId = request.SalesInvoiceId,
            PartId = request.PartId,
            InsuranceOptionId = request.InsuranceOptionId,
            OptionName = option.Name,
            Price = price,
            Currency = option.Currency,
            CreatedAt = now
        };
    }
}
