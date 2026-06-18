using System.ComponentModel.DataAnnotations;
using Dapper;
using SpareParts.Domain.Pricing;
using SpareParts.Infrastructure.Data;
using SpareParts.Infrastructure.Interfaces;

namespace SpareParts.Infrastructure.Services;

public sealed class NewVsUsedService
{
    private readonly ISqlConnectionFactory _factory;
    private readonly ITenantContext _tenantContext;

    public NewVsUsedService(ISqlConnectionFactory factory, ITenantContext tenantContext)
    {
        _factory = factory;
        _tenantContext = tenantContext;
    }

    public NewVsUsedDto? GetComparison(int partId)
    {
        using var session = new DbSession(_factory, _tenantContext.TenantId);

        var row = session.Connection.QuerySingleOrDefault<NewVsUsedRow>(
            """
SELECT Id, Name, SalePrice, OemNewPrice, AftermarketNewPrice, Currency
FROM dbo.Parts
WHERE Id = @PartId AND (@TenantId = 0 OR TenantId = @TenantId);
""",
            new { PartId = partId, TenantId = _tenantContext.TenantId },
            session.Transaction);

        if (row is null) return null;

        var dto = new NewVsUsedDto
        {
            PartId = partId,
            PartName = row.Name,
            UsedPrice = row.SalePrice > 0 ? row.SalePrice : null,
            OemNewPrice = row.OemNewPrice,
            AftermarketNewPrice = row.AftermarketNewPrice,
            Currency = row.Currency ?? "USD"
        };

        if (dto.UsedPrice.HasValue && dto.OemNewPrice.HasValue && dto.OemNewPrice.Value > 0)
        {
            dto.SavingsVsOem = Math.Round(dto.OemNewPrice.Value - dto.UsedPrice.Value, 2);
            dto.SavingsPercentVsOem = Math.Round((dto.SavingsVsOem.Value / dto.OemNewPrice.Value) * 100m, 1);
        }

        if (dto.UsedPrice.HasValue && dto.AftermarketNewPrice.HasValue && dto.AftermarketNewPrice.Value > 0)
        {
            dto.SavingsVsAftermarket = Math.Round(dto.AftermarketNewPrice.Value - dto.UsedPrice.Value, 2);
            dto.SavingsPercentVsAftermarket = Math.Round((dto.SavingsVsAftermarket.Value / dto.AftermarketNewPrice.Value) * 100m, 1);
        }

        return dto;
    }

    public void UpdateNewPrices(UpdateNewPricesRequest request, int modifiedByUserId)
    {
        if (request.PartId <= 0)
            throw new ValidationException("A valid part must be specified.");

        using var session = new DbSession(_factory, _tenantContext.TenantId);

        session.Connection.Execute(
            """
UPDATE dbo.Parts
SET OemNewPrice = @OemNewPrice,
    AftermarketNewPrice = @AftermarketNewPrice,
    ModifiedAt = @Now,
    ModifiedByUserId = @ModifiedByUserId
WHERE Id = @PartId AND (@TenantId = 0 OR TenantId = @TenantId);
""",
            new
            {
                PartId = request.PartId,
                OemNewPrice = request.OemNewPrice,
                AftermarketNewPrice = request.AftermarketNewPrice,
                Now = DateTime.UtcNow,
                ModifiedByUserId = modifiedByUserId,
                TenantId = _tenantContext.TenantId
            },
            session.Transaction);

        session.Commit();
    }
}
