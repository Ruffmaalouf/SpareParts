using System.Data;
using Dapper;
using SpareParts.Domain.Pricing;
using SpareParts.Infrastructure.Data;
using SpareParts.Infrastructure.Interfaces;

namespace SpareParts.Infrastructure.Services.Pricing;

public sealed class SubscriptionLimitService : ISubscriptionLimitService
{
    private const string UpgradeUrl = "/billing/pricing";

    private readonly ISqlConnectionFactory _factory;

    public SubscriptionLimitService(ISqlConnectionFactory factory)
    {
        _factory = factory;
    }

    public string GetActivePackageCode(int tenantId)
    {
        using var conn = _factory.CreateConnection();
        return GetEffectivePackage(conn, tenantId).Code;
    }

    public bool HasFeature(int tenantId, string featureCode)
    {
        using var conn = _factory.CreateConnection();
        var package = GetEffectivePackage(conn, tenantId);
        return IsFeatureEnabled(conn, null, package.Id, featureCode);
    }

    public void EnsureFeatureEnabled(int tenantId, string featureCode, string featureDisplayName)
    {
        using var conn = _factory.CreateConnection();
        var package = GetEffectivePackage(conn, tenantId);

        if (IsFeatureEnabled(conn, null, package.Id, featureCode))
        {
            return;
        }

        var requiredPlans = conn.Query<string>(
            """
            SELECT pp.Code
            FROM dbo.PricingPackages pp
            INNER JOIN dbo.PackageFeatures pf ON pf.PackageId = pp.Id
            WHERE pp.IsActive = 1
              AND pp.SortOrder > @CurrentSortOrder
              AND pf.FeatureCode = @FeatureCode
              AND pf.IsEnabled = 1
            ORDER BY pp.SortOrder
            """,
            new { CurrentSortOrder = package.SortOrder, FeatureCode = featureCode }).ToList();

        throw new PlanLockException(
            $"{featureDisplayName} is not available on your current plan.",
            PlanLockErrorCode.FeatureLocked,
            package.Code,
            requiredPlans,
            UpgradeUrl);
    }

    public int GetLimitValue(int tenantId, string limitCode)
    {
        using var conn = _factory.CreateConnection();
        var package = GetEffectivePackage(conn, tenantId);
        return GetLimitValue(conn, null, package.Id, limitCode);
    }

    public void EnsureWithinLimit(int tenantId, string limitCode, int currentCount, string limitDisplayName, int additional = 1)
    {
        using var conn = _factory.CreateConnection();
        var package = GetEffectivePackage(conn, tenantId);
        var limitValue = GetLimitValue(conn, null, package.Id, limitCode);

        if (limitValue == LimitCode.Unlimited || currentCount + additional <= limitValue)
        {
            return;
        }

        var requiredPlans = FindUpgradePlansForLimit(conn, null, package.SortOrder, limitCode, currentCount + additional);

        throw new PlanLockException(
            $"Your plan allows up to {limitValue} {limitDisplayName}. Upgrade your plan to add more.",
            PlanLockErrorCode.PlanLimitExceeded,
            package.Code,
            requiredPlans,
            UpgradeUrl);
    }

    public int GetMonthlyUsage(int tenantId, string counterCode)
    {
        using var conn = _factory.CreateConnection();
        var now = DateTime.UtcNow;
        return conn.ExecuteScalar<int?>(
            """
            SELECT CurrentValue FROM dbo.TenantUsageCounters
            WHERE TenantId = @TenantId AND CounterCode = @CounterCode AND PeriodYear = @Year AND PeriodMonth = @Month
            """,
            new { TenantId = tenantId, CounterCode = counterCode, Year = now.Year, Month = now.Month }) ?? 0;
    }

    public int EnsureAndIncrementMonthlyUsage(int tenantId, string counterCode, string limitDisplayName, int additional = 1)
    {
        using var session = new DbSession(_factory);
        var package = GetEffectivePackage(session.Connection, tenantId, session.Transaction);
        var limitValue = GetLimitValue(session.Connection, session.Transaction, package.Id, counterCode);

        var now = DateTime.UtcNow;
        var current = session.Connection.ExecuteScalar<int?>(
            """
            SELECT CurrentValue FROM dbo.TenantUsageCounters
            WHERE TenantId = @TenantId AND CounterCode = @CounterCode AND PeriodYear = @Year AND PeriodMonth = @Month
            """,
            new { TenantId = tenantId, CounterCode = counterCode, Year = now.Year, Month = now.Month },
            session.Transaction) ?? 0;

        if (limitValue != LimitCode.Unlimited && current + additional > limitValue)
        {
            var requiredPlans = FindUpgradePlansForLimit(session.Connection, session.Transaction, package.SortOrder, counterCode, current + additional);

            throw new PlanLockException(
                $"Your plan allows up to {limitValue} {limitDisplayName} per month. Upgrade your plan to continue.",
                PlanLockErrorCode.PlanLimitExceeded,
                package.Code,
                requiredPlans,
                UpgradeUrl);
        }

        var newValue = current + additional;
        session.Connection.Execute(
            """
            MERGE dbo.TenantUsageCounters AS target
            USING (SELECT @TenantId AS TenantId, @CounterCode AS CounterCode, @Year AS PeriodYear, @Month AS PeriodMonth) AS src
                ON target.TenantId = src.TenantId
               AND target.CounterCode = src.CounterCode
               AND target.PeriodYear = src.PeriodYear
               AND target.PeriodMonth = src.PeriodMonth
            WHEN MATCHED THEN
                UPDATE SET CurrentValue = target.CurrentValue + @Additional, ModifiedAt = @Now
            WHEN NOT MATCHED THEN
                INSERT (TenantId, CounterCode, PeriodYear, PeriodMonth, CurrentValue, CreatedAt)
                VALUES (@TenantId, @CounterCode, @Year, @Month, @Additional, @Now);
            """,
            new
            {
                TenantId = tenantId,
                CounterCode = counterCode,
                Year = now.Year,
                Month = now.Month,
                Additional = additional,
                Now = now
            },
            session.Transaction);

        session.Commit();
        return newValue;
    }

    private static bool IsFeatureEnabled(IDbConnection conn, IDbTransaction? transaction, int packageId, string featureCode)
    {
        return conn.ExecuteScalar<bool?>(
            "SELECT IsEnabled FROM dbo.PackageFeatures WHERE PackageId = @PackageId AND FeatureCode = @FeatureCode",
            new { PackageId = packageId, FeatureCode = featureCode },
            transaction) ?? false;
    }

    private static int GetLimitValue(IDbConnection conn, IDbTransaction? transaction, int packageId, string limitCode)
    {
        return conn.ExecuteScalar<int?>(
            "SELECT LimitValue FROM dbo.PackageLimits WHERE PackageId = @PackageId AND LimitCode = @LimitCode",
            new { PackageId = packageId, LimitCode = limitCode },
            transaction) ?? 0;
    }

    private static List<string> FindUpgradePlansForLimit(IDbConnection conn, IDbTransaction? transaction, int currentSortOrder, string limitCode, int requiredValue)
    {
        return conn.Query<string>(
            """
            SELECT pp.Code
            FROM dbo.PricingPackages pp
            INNER JOIN dbo.PackageLimits pl ON pl.PackageId = pp.Id
            WHERE pp.IsActive = 1
              AND pp.SortOrder > @CurrentSortOrder
              AND pl.LimitCode = @LimitCode
              AND (pl.LimitValue = -1 OR pl.LimitValue >= @RequiredValue)
            ORDER BY pp.SortOrder
            """,
            new { CurrentSortOrder = currentSortOrder, LimitCode = limitCode, RequiredValue = requiredValue },
            transaction).ToList();
    }

    private static (int Id, string Code, int SortOrder) GetEffectivePackage(IDbConnection conn, int tenantId, IDbTransaction? transaction = null)
    {
        var row = conn.QuerySingleOrDefault<SubscriptionPackageRow>(
            """
            SELECT pp.Id, pp.Code, pp.SortOrder, ts.Status, ts.TrialEndsAt, ts.CurrentPeriodEnd
            FROM dbo.TenantSubscriptions ts
            INNER JOIN dbo.PricingPackages pp ON pp.Id = ts.PackageId
            WHERE ts.TenantId = @TenantId
            """,
            new { TenantId = tenantId },
            transaction);

        var now = DateTime.UtcNow;
        var isExpired = row is null
            || row.Status is (int)SubscriptionStatus.Cancelled or (int)SubscriptionStatus.Expired or (int)SubscriptionStatus.PastDue
            || (row.Status == (int)SubscriptionStatus.Trialing && row.TrialEndsAt is { } trialEnd && trialEnd < now)
            || (row.Status == (int)SubscriptionStatus.Active && row.CurrentPeriodEnd < now);

        if (!isExpired && row is not null)
        {
            return (row.Id, row.Code, row.SortOrder);
        }

        var free = conn.QuerySingleOrDefault<SubscriptionLimitPackageRow>(
            "SELECT Id, Code, SortOrder FROM dbo.PricingPackages WHERE Code = @Code",
            new { Code = PackageCode.Free },
            transaction);

        if (free is null)
        {
            throw new InvalidOperationException("The FREE pricing package is not seeded.");
        }

        return (free.Id, free.Code, free.SortOrder);
    }
}
