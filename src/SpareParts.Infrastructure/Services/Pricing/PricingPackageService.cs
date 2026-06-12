using Dapper;
using SpareParts.Domain.Pricing;
using SpareParts.Infrastructure.Data;
using SpareParts.Infrastructure.Interfaces;

namespace SpareParts.Infrastructure.Services.Pricing;

public sealed class PricingPackageService : IPricingPackageService
{
    private readonly ISqlConnectionFactory _factory;

    public PricingPackageService(ISqlConnectionFactory factory)
    {
        _factory = factory;
    }

    public IReadOnlyList<PricingPackageDto> GetAll(bool activeOnly = true)
    {
        using var conn = _factory.CreateConnection();
        var packages = conn.Query<PricingPackage>(
            "SELECT * FROM dbo.PricingPackages WHERE (@ActiveOnly = 0 OR IsActive = 1) ORDER BY SortOrder, Id",
            new { ActiveOnly = activeOnly }).ToList();

        if (packages.Count == 0)
        {
            return [];
        }

        var packageIds = packages.Select(p => p.Id).ToArray();
        var features = conn.Query<PackageFeature>(
            "SELECT * FROM dbo.PackageFeatures WHERE PackageId IN @PackageIds",
            new { PackageIds = packageIds }).ToList();
        var limits = conn.Query<PackageLimit>(
            "SELECT * FROM dbo.PackageLimits WHERE PackageId IN @PackageIds",
            new { PackageIds = packageIds }).ToList();

        return packages.Select(p => MapToDto(p, features, limits)).ToList();
    }

    public PricingPackageDto GetByCode(string code)
    {
        using var conn = _factory.CreateConnection();
        var package = conn.QuerySingleOrDefault<PricingPackage>(
            "SELECT * FROM dbo.PricingPackages WHERE Code = @Code",
            new { Code = code });

        if (package is null)
        {
            throw new NotFoundException($"Pricing package '{code}' was not found.");
        }

        return LoadDto(conn, package);
    }

    public PricingPackageDto GetById(int id)
    {
        using var conn = _factory.CreateConnection();
        var package = conn.QuerySingleOrDefault<PricingPackage>(
            "SELECT * FROM dbo.PricingPackages WHERE Id = @Id",
            new { Id = id });

        if (package is null)
        {
            throw new NotFoundException($"Pricing package {id} was not found.");
        }

        return LoadDto(conn, package);
    }

    public int Create(UpsertPricingPackageRequest request, int? userId)
    {
        ValidateRequest(request);

        using var session = new DbSession(_factory);

        var existing = session.Connection.ExecuteScalar<int>(
            "SELECT COUNT(1) FROM dbo.PricingPackages WHERE Code = @Code",
            new { request.Code },
            session.Transaction);
        if (existing > 0)
        {
            throw new ConflictException($"A pricing package with code '{request.Code}' already exists.");
        }

        var id = session.Connection.ExecuteScalar<int>(
            """
            INSERT INTO dbo.PricingPackages (Code, Name, Description, MonthlyPrice, YearlyPrice, Currency, SortOrder, IsActive, IsCustomPricing, CreatedAt, CreatedByUserId)
            VALUES (@Code, @Name, @Description, @MonthlyPrice, @YearlyPrice, @Currency, @SortOrder, @IsActive, @IsCustomPricing, @Now, @UserId);
            SELECT CAST(SCOPE_IDENTITY() AS INT);
            """,
            new
            {
                request.Code,
                request.Name,
                request.Description,
                request.MonthlyPrice,
                request.YearlyPrice,
                request.Currency,
                request.SortOrder,
                request.IsActive,
                request.IsCustomPricing,
                Now = DateTime.UtcNow,
                UserId = userId
            },
            session.Transaction);

        SaveFeaturesAndLimits(session, id, request);
        session.Commit();
        return id;
    }

    public void Update(int id, UpsertPricingPackageRequest request, int? userId)
    {
        ValidateRequest(request);

        using var session = new DbSession(_factory);

        var duplicateCode = session.Connection.ExecuteScalar<int>(
            "SELECT COUNT(1) FROM dbo.PricingPackages WHERE Code = @Code AND Id <> @Id",
            new { request.Code, Id = id },
            session.Transaction);
        if (duplicateCode > 0)
        {
            throw new ConflictException($"A pricing package with code '{request.Code}' already exists.");
        }

        var affected = session.Connection.Execute(
            """
            UPDATE dbo.PricingPackages
            SET Code = @Code,
                Name = @Name,
                Description = @Description,
                MonthlyPrice = @MonthlyPrice,
                YearlyPrice = @YearlyPrice,
                Currency = @Currency,
                SortOrder = @SortOrder,
                IsActive = @IsActive,
                IsCustomPricing = @IsCustomPricing,
                ModifiedAt = @Now,
                ModifiedByUserId = @UserId
            WHERE Id = @Id
            """,
            new
            {
                Id = id,
                request.Code,
                request.Name,
                request.Description,
                request.MonthlyPrice,
                request.YearlyPrice,
                request.Currency,
                request.SortOrder,
                request.IsActive,
                request.IsCustomPricing,
                Now = DateTime.UtcNow,
                UserId = userId
            },
            session.Transaction);

        if (affected == 0)
        {
            throw new NotFoundException($"Pricing package {id} was not found.");
        }

        session.Connection.Execute("DELETE FROM dbo.PackageFeatures WHERE PackageId = @Id", new { Id = id }, session.Transaction);
        session.Connection.Execute("DELETE FROM dbo.PackageLimits WHERE PackageId = @Id", new { Id = id }, session.Transaction);
        SaveFeaturesAndLimits(session, id, request);

        session.Commit();
    }

    public void SetActive(int id, bool isActive, int? userId)
    {
        using var conn = _factory.CreateConnection();
        var affected = conn.Execute(
            "UPDATE dbo.PricingPackages SET IsActive = @IsActive, ModifiedAt = @Now, ModifiedByUserId = @UserId WHERE Id = @Id",
            new { Id = id, IsActive = isActive, Now = DateTime.UtcNow, UserId = userId });

        if (affected == 0)
        {
            throw new NotFoundException($"Pricing package {id} was not found.");
        }
    }

    private static void ValidateRequest(UpsertPricingPackageRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
        {
            throw new ValidationException("Package code is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ValidationException("Package name is required.");
        }

        var invalidFeatures = request.Features
            .Select(f => f.FeatureCode)
            .Where(code => !FeatureCode.All.Contains(code, StringComparer.Ordinal))
            .ToList();
        if (invalidFeatures.Count > 0)
        {
            throw new ValidationException($"Unknown feature code(s): {string.Join(", ", invalidFeatures)}.");
        }

        var invalidLimits = request.Limits
            .Select(l => l.LimitCode)
            .Where(code => !LimitCode.All.Contains(code, StringComparer.Ordinal))
            .ToList();
        if (invalidLimits.Count > 0)
        {
            throw new ValidationException($"Unknown limit code(s): {string.Join(", ", invalidLimits)}.");
        }
    }

    private static void SaveFeaturesAndLimits(DbSession session, int packageId, UpsertPricingPackageRequest request)
    {
        if (request.Features.Count > 0)
        {
            session.Connection.Execute(
                """
                INSERT INTO dbo.PackageFeatures (PackageId, FeatureCode, IsEnabled, CreatedAt)
                VALUES (@PackageId, @FeatureCode, @IsEnabled, @Now)
                """,
                request.Features.Select(f => new { PackageId = packageId, f.FeatureCode, f.IsEnabled, Now = DateTime.UtcNow }),
                session.Transaction);
        }

        if (request.Limits.Count > 0)
        {
            session.Connection.Execute(
                """
                INSERT INTO dbo.PackageLimits (PackageId, LimitCode, LimitValue, CreatedAt)
                VALUES (@PackageId, @LimitCode, @LimitValue, @Now)
                """,
                request.Limits.Select(l => new { PackageId = packageId, l.LimitCode, l.LimitValue, Now = DateTime.UtcNow }),
                session.Transaction);
        }
    }

    private static PricingPackageDto LoadDto(System.Data.IDbConnection conn, PricingPackage package)
    {
        var features = conn.Query<PackageFeature>(
            "SELECT * FROM dbo.PackageFeatures WHERE PackageId = @PackageId",
            new { PackageId = package.Id }).ToList();
        var limits = conn.Query<PackageLimit>(
            "SELECT * FROM dbo.PackageLimits WHERE PackageId = @PackageId",
            new { PackageId = package.Id }).ToList();

        return MapToDto(package, features, limits);
    }

    private static PricingPackageDto MapToDto(PricingPackage package, List<PackageFeature> features, List<PackageLimit> limits)
    {
        return new PricingPackageDto
        {
            Id = package.Id,
            Code = package.Code,
            Name = package.Name,
            Description = package.Description,
            MonthlyPrice = package.MonthlyPrice,
            YearlyPrice = package.YearlyPrice,
            Currency = package.Currency,
            SortOrder = package.SortOrder,
            IsActive = package.IsActive,
            IsCustomPricing = package.IsCustomPricing,
            Features = features
                .Where(f => f.PackageId == package.Id)
                .Select(f => new PackageFeatureDto { FeatureCode = f.FeatureCode, IsEnabled = f.IsEnabled })
                .ToList(),
            Limits = limits
                .Where(l => l.PackageId == package.Id)
                .Select(l => new PackageLimitDto { LimitCode = l.LimitCode, LimitValue = l.LimitValue })
                .ToList()
        };
    }
}
