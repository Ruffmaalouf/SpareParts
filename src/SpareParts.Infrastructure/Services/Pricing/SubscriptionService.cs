using System.Data;
using System.Globalization;
using Dapper;
using SpareParts.Domain.Pricing;
using SpareParts.Infrastructure.Data;
using SpareParts.Infrastructure.Interfaces;

namespace SpareParts.Infrastructure.Services.Pricing;

public sealed class SubscriptionService : ISubscriptionService
{
    private readonly ISqlConnectionFactory _factory;
    private readonly IPaymentProviderFactory _providerFactory;
    private readonly IInvoiceService _invoiceService;
    private readonly PaymentSettings _settings;

    public SubscriptionService(
        ISqlConnectionFactory factory,
        IPaymentProviderFactory providerFactory,
        IInvoiceService invoiceService,
        PaymentSettings settings)
    {
        _factory = factory;
        _providerFactory = providerFactory;
        _invoiceService = invoiceService;
        _settings = settings;
    }

    public TenantSubscriptionDto GetCurrent(int tenantId)
    {
        using var session = new DbSession(_factory);
        var row = LoadSubscriptionRow(session.Connection, tenantId, session.Transaction);

        if (row is null)
        {
            var freePackage = GetPackageByCode(session.Connection, PackageCode.Free, session.Transaction);
            var now = DateTime.UtcNow;
            UpsertSubscription(session, tenantId, freePackage.Id, SubscriptionStatus.Active, BillingCycle.Monthly, now, FarFutureEnd(now), null, null);
            row = LoadSubscriptionRow(session.Connection, tenantId, session.Transaction)!;
        }

        var dto = BuildDto(session.Connection, row, session.Transaction);
        session.Commit();
        return dto;
    }

    public TenantSubscriptionDto StartFree(int tenantId, int userId)
    {
        using var session = new DbSession(_factory);
        var row = LoadSubscriptionRow(session.Connection, tenantId, session.Transaction);

        if (row is not null)
        {
            if (row.PackageCode == PackageCode.Free)
            {
                var existingDto = BuildDto(session.Connection, row, session.Transaction);
                session.Commit();
                return existingDto;
            }

            throw new ConflictException("This tenant already has a subscription. Use change-plan or cancel instead.");
        }

        var freePackage = GetPackageByCode(session.Connection, PackageCode.Free, session.Transaction);
        var now = DateTime.UtcNow;
        UpsertSubscription(session, tenantId, freePackage.Id, SubscriptionStatus.Active, BillingCycle.Monthly, now, FarFutureEnd(now), userId, null);

        var newRow = LoadSubscriptionRow(session.Connection, tenantId, session.Transaction)!;
        var dto = BuildDto(session.Connection, newRow, session.Transaction);
        session.Commit();
        return dto;
    }

    public TenantSubscriptionDto StartTrial(int tenantId, StartTrialRequest request, int userId)
    {
        var packageCode = string.IsNullOrWhiteSpace(request.PackageCode) ? PackageCode.Pro : request.PackageCode;

        if (!PackageCode.All.Contains(packageCode, StringComparer.Ordinal) || packageCode == PackageCode.Free)
        {
            throw new ValidationException($"Cannot start a trial for package '{packageCode}'.");
        }

        var trialDays = request.TrialDays > 0 ? request.TrialDays : 14;
        if (trialDays > 90)
        {
            throw new ValidationException("Trial period cannot exceed 90 days.");
        }

        using var session = new DbSession(_factory);
        var now = DateTime.UtcNow;
        var row = LoadSubscriptionRow(session.Connection, tenantId, session.Transaction);

        if (row is not null)
        {
            var status = (SubscriptionStatus)row.Status;
            var effectiveStatus = SubscriptionStatusCalculator.ResolveEffectiveStatus(status, row.CurrentPeriodEnd, row.TrialEndsAt, now);
            var hasAccess = SubscriptionStatusCalculator.GrantsPlanAccess(effectiveStatus);

            if (hasAccess && row.PackageCode != PackageCode.Free)
            {
                throw new ConflictException("This tenant already has an active subscription.");
            }
        }

        var package = GetPackageByCode(session.Connection, packageCode, session.Transaction);
        var trialEnd = now.AddDays(trialDays);

        UpsertSubscription(session, tenantId, package.Id, SubscriptionStatus.Trialing, BillingCycle.Monthly, now, trialEnd, userId, trialEnd);

        var newRow = LoadSubscriptionRow(session.Connection, tenantId, session.Transaction)!;
        var dto = BuildDto(session.Connection, newRow, session.Transaction);
        session.Commit();
        return dto;
    }

    public async Task<CheckoutResponseDto> CheckoutAsync(int tenantId, CheckoutRequest request, int userId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.PackageCode)
            || !PackageCode.All.Contains(request.PackageCode, StringComparer.Ordinal)
            || request.PackageCode == PackageCode.Free)
        {
            throw new ValidationException("A valid paid package code is required for checkout.");
        }

        if (!Enum.TryParse<BillingCycle>(request.BillingCycle, true, out var billingCycle))
        {
            throw new ValidationException($"Unknown billing cycle '{request.BillingCycle}'.");
        }

        SubscriptionPricingPackageRow package;
        using (var conn = _factory.CreateConnection())
        {
            package = GetPackageByCode(conn, request.PackageCode, null);
        }

        if (!package.IsActive)
        {
            throw new ValidationException($"The {package.Name} plan is not currently available.");
        }

        if (package.IsCustomPricing)
        {
            throw new ValidationException($"The {package.Name} plan uses custom pricing. Please contact sales to subscribe.");
        }

        var amount = billingCycle == BillingCycle.Yearly ? package.YearlyPrice : package.MonthlyPrice;
        var providerCode = string.IsNullOrWhiteSpace(request.ProviderCode) ? _settings.DefaultProvider : request.ProviderCode;
        var provider = _providerFactory.GetProvider(providerCode);

        int paymentId;
        using (var session = new DbSession(_factory))
        {
            var subscriptionId = session.Connection.ExecuteScalar<int?>(
                "SELECT Id FROM dbo.TenantSubscriptions WHERE TenantId = @TenantId",
                new { TenantId = tenantId },
                session.Transaction);

            paymentId = session.Connection.ExecuteScalar<int>(
                """
                INSERT INTO dbo.Payments (TenantId, SubscriptionId, ProviderCode, PackageCode, BillingCycle, Amount, Currency, Status, CreatedAt, CreatedByUserId)
                VALUES (@TenantId, @SubscriptionId, @ProviderCode, @PackageCode, @BillingCycle, @Amount, @Currency, @Status, @Now, @UserId);
                SELECT CAST(SCOPE_IDENTITY() AS INT);
                """,
                new
                {
                    TenantId = tenantId,
                    SubscriptionId = subscriptionId,
                    ProviderCode = provider.ProviderCode,
                    request.PackageCode,
                    BillingCycle = (int)billingCycle,
                    Amount = amount,
                    Currency = package.Currency,
                    Status = (int)PaymentStatus.Pending,
                    Now = DateTime.UtcNow,
                    UserId = userId
                },
                session.Transaction);

            session.Commit();
        }

        var checkoutContext = new PaymentCheckoutContext
        {
            PaymentId = paymentId,
            TenantId = tenantId,
            PackageCode = request.PackageCode,
            BillingCycle = billingCycle.ToString(),
            Amount = amount,
            Currency = package.Currency,
            SuccessUrl = AppendQuery(BuildAbsoluteUrl(_settings.SuccessUrl), "paymentId", paymentId.ToString(CultureInfo.InvariantCulture)),
            CancelUrl = AppendQuery(BuildAbsoluteUrl(_settings.CancelUrl), "paymentId", paymentId.ToString(CultureInfo.InvariantCulture))
        };

        CheckoutResponseDto response;
        try
        {
            response = await provider.CreateCheckoutAsync(checkoutContext, cancellationToken);
        }
        catch (DomainException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new ExternalServiceException($"Failed to create checkout session: {ex.Message}");
        }

        using (var session = new DbSession(_factory))
        {
            switch (response.Status)
            {
                case "redirect":
                    session.Connection.Execute(
                        "UPDATE dbo.Payments SET ProviderCheckoutSessionId = @SessionId, ModifiedAt = @Now WHERE Id = @Id",
                        new { SessionId = response.CheckoutSessionId, Now = DateTime.UtcNow, Id = paymentId },
                        session.Transaction);
                    PaymentTransactionLog.Insert(session.Connection, session.Transaction, paymentId, provider.ProviderCode, PaymentTransactionStatus.Initiated, "Checkout session created.");
                    break;

                case "succeeded":
                    session.Connection.Execute(
                        "UPDATE dbo.Payments SET Status = @Status, PaidAt = @Now, ModifiedAt = @Now WHERE Id = @Id",
                        new { Status = (int)PaymentStatus.Succeeded, Now = DateTime.UtcNow, Id = paymentId },
                        session.Transaction);
                    PaymentTransactionLog.Insert(session.Connection, session.Transaction, paymentId, provider.ProviderCode, PaymentTransactionStatus.Succeeded, "Payment succeeded.");
                    break;

                case "failed":
                    session.Connection.Execute(
                        "UPDATE dbo.Payments SET Status = @Status, ModifiedAt = @Now WHERE Id = @Id",
                        new { Status = (int)PaymentStatus.Failed, Now = DateTime.UtcNow, Id = paymentId },
                        session.Transaction);
                    PaymentTransactionLog.Insert(session.Connection, session.Transaction, paymentId, provider.ProviderCode, PaymentTransactionStatus.Failed, "Payment failed.");
                    break;

                default:
                    PaymentTransactionLog.Insert(session.Connection, session.Transaction, paymentId, provider.ProviderCode, PaymentTransactionStatus.Pending, "Awaiting manual payment confirmation.");
                    break;
            }

            session.Commit();
        }

        if (response.Status == "succeeded")
        {
            AdminActivate(new AdminActivateSubscriptionRequest
            {
                TenantId = tenantId,
                PackageCode = request.PackageCode,
                BillingCycle = billingCycle.ToString(),
                PaymentId = paymentId
            }, userId);

            _invoiceService.CreateForPayment(paymentId, userId);
        }

        response.PaymentId = paymentId;
        response.ProviderCode = provider.ProviderCode;
        return response;
    }

    public TenantSubscriptionDto ChangePlan(int tenantId, ChangePlanRequest request, int userId)
    {
        if (string.IsNullOrWhiteSpace(request.PackageCode) || !PackageCode.All.Contains(request.PackageCode, StringComparer.Ordinal))
        {
            throw new ValidationException($"Unknown package code '{request.PackageCode}'.");
        }

        using var session = new DbSession(_factory);
        var now = DateTime.UtcNow;
        var row = LoadSubscriptionRow(session.Connection, tenantId, session.Transaction);
        var targetPackage = GetPackageByCode(session.Connection, request.PackageCode, session.Transaction);

        var billingCycle = row is not null ? (BillingCycle)row.BillingCycle : BillingCycle.Monthly;
        if (!string.IsNullOrWhiteSpace(request.BillingCycle) && !Enum.TryParse(request.BillingCycle, true, out billingCycle))
        {
            throw new ValidationException($"Unknown billing cycle '{request.BillingCycle}'.");
        }

        if (row is not null && targetPackage.SortOrder > row.PackageSortOrder && request.PackageCode != PackageCode.Free)
        {
            throw new ValidationException("Upgrading to a higher plan requires checkout. Use POST /api/subscription/checkout.");
        }

        var periodStart = row?.CurrentPeriodStart ?? now;
        var periodEnd = request.PackageCode == PackageCode.Free
            ? FarFutureEnd(now)
            : row?.CurrentPeriodEnd ?? (billingCycle == BillingCycle.Yearly ? now.AddYears(1) : now.AddMonths(1));

        UpsertSubscription(session, tenantId, targetPackage.Id, SubscriptionStatus.Active, billingCycle, periodStart, periodEnd, userId, null);

        var newRow = LoadSubscriptionRow(session.Connection, tenantId, session.Transaction)!;
        var dto = BuildDto(session.Connection, newRow, session.Transaction);
        session.Commit();
        return dto;
    }

    public void Cancel(int tenantId, CancelSubscriptionRequest request, int userId)
    {
        using var session = new DbSession(_factory);
        var row = LoadSubscriptionRow(session.Connection, tenantId, session.Transaction);

        if (row is null || row.PackageCode == PackageCode.Free)
        {
            throw new ValidationException("There is no active paid subscription to cancel.");
        }

        var now = DateTime.UtcNow;

        if (request.Immediate)
        {
            var freePackage = GetPackageByCode(session.Connection, PackageCode.Free, session.Transaction);
            session.Connection.Execute(
                """
                UPDATE dbo.TenantSubscriptions
                SET PackageId = @PackageId, Status = @Status, BillingCycle = @BillingCycle,
                    CurrentPeriodStart = @Now, CurrentPeriodEnd = @PeriodEnd, TrialEndsAt = NULL,
                    CancelledAt = @Now, CancelAtPeriodEnd = 0, ProviderCode = NULL, ProviderSubscriptionId = NULL,
                    ModifiedAt = @Now, ModifiedByUserId = @UserId
                WHERE Id = @Id
                """,
                new
                {
                    row.Id,
                    PackageId = freePackage.Id,
                    Status = (int)SubscriptionStatus.Active,
                    BillingCycle = (int)BillingCycle.Monthly,
                    Now = now,
                    PeriodEnd = FarFutureEnd(now),
                    UserId = userId
                },
                session.Transaction);
        }
        else
        {
            session.Connection.Execute(
                "UPDATE dbo.TenantSubscriptions SET CancelAtPeriodEnd = 1, CancelledAt = @Now, ModifiedAt = @Now, ModifiedByUserId = @UserId WHERE Id = @Id",
                new { row.Id, Now = now, UserId = userId },
                session.Transaction);
        }

        session.Commit();
    }

    public IReadOnlyList<TenantSubscriptionDto> GetAllForAdmin()
    {
        using var conn = _factory.CreateConnection();
        var rows = conn.Query<SubscriptionRow>(SubscriptionRowSelect + " ORDER BY t.Name").ToList();
        return rows.Select(row => BuildDto(conn, row, null)).ToList();
    }

    public TenantSubscriptionDto AdminActivate(AdminActivateSubscriptionRequest request, int userId)
    {
        if (!PackageCode.All.Contains(request.PackageCode, StringComparer.Ordinal))
        {
            throw new ValidationException($"Unknown package code '{request.PackageCode}'.");
        }

        if (!Enum.TryParse<BillingCycle>(request.BillingCycle, true, out var billingCycle))
        {
            throw new ValidationException($"Unknown billing cycle '{request.BillingCycle}'.");
        }

        using var session = new DbSession(_factory);

        if (!TenantExists(session.Connection, request.TenantId, session.Transaction))
        {
            throw new NotFoundException($"Tenant {request.TenantId} was not found.");
        }

        var package = GetPackageByCode(session.Connection, request.PackageCode, session.Transaction);
        var now = DateTime.UtcNow;
        var periodEnd = ResolvePeriodEnd(now, request.PackageCode, billingCycle, request.DurationDays);

        UpsertSubscription(session, request.TenantId, package.Id, SubscriptionStatus.Active, billingCycle, now, periodEnd, userId, null);

        if (request.PaymentId is { } paymentId)
        {
            session.Connection.Execute(
                """
                UPDATE dbo.Payments
                SET SubscriptionId = (SELECT Id FROM dbo.TenantSubscriptions WHERE TenantId = @TenantId), ModifiedAt = @Now
                WHERE Id = @PaymentId
                """,
                new { request.TenantId, PaymentId = paymentId, Now = now },
                session.Transaction);
        }

        var row = LoadSubscriptionRow(session.Connection, request.TenantId, session.Transaction)!;
        var dto = BuildDto(session.Connection, row, session.Transaction);
        session.Commit();
        return dto;
    }

    public int RunMaintenance()
    {
        using var session = new DbSession(_factory);
        var now = DateTime.UtcNow;
        var freePackage = GetPackageByCode(session.Connection, PackageCode.Free, session.Transaction);
        var changed = 0;

        // Subscriptions cancelled-at-period-end whose period has now ended: downgrade to FREE gracefully.
        changed += session.Connection.Execute(
            """
            UPDATE dbo.TenantSubscriptions
            SET PackageId = @FreePackageId, Status = @ActiveStatus, BillingCycle = @MonthlyCycle,
                CurrentPeriodStart = @Now, CurrentPeriodEnd = @FarFuture, TrialEndsAt = NULL,
                CancelAtPeriodEnd = 0, ProviderCode = NULL, ProviderSubscriptionId = NULL, ModifiedAt = @Now
            WHERE Status = @ActiveStatus AND CurrentPeriodEnd < @Now AND CancelAtPeriodEnd = 1
            """,
            new
            {
                FreePackageId = freePackage.Id,
                ActiveStatus = (int)SubscriptionStatus.Active,
                MonthlyCycle = (int)BillingCycle.Monthly,
                Now = now,
                FarFuture = FarFutureEnd(now)
            },
            session.Transaction);

        // Active subscriptions whose period ended without an upcoming payment become PastDue (grace period).
        changed += session.Connection.Execute(
            """
            UPDATE dbo.TenantSubscriptions
            SET Status = @PastDueStatus, ModifiedAt = @Now
            WHERE Status = @ActiveStatus AND CurrentPeriodEnd < @Now AND CancelAtPeriodEnd = 0
            """,
            new { PastDueStatus = (int)SubscriptionStatus.PastDue, ActiveStatus = (int)SubscriptionStatus.Active, Now = now },
            session.Transaction);

        // PastDue subscriptions beyond the grace period are marked Expired.
        changed += session.Connection.Execute(
            """
            UPDATE dbo.TenantSubscriptions
            SET Status = @ExpiredStatus, ModifiedAt = @Now
            WHERE Status = @PastDueStatus AND DATEADD(DAY, @GraceDays, CurrentPeriodEnd) < @Now
            """,
            new
            {
                ExpiredStatus = (int)SubscriptionStatus.Expired,
                PastDueStatus = (int)SubscriptionStatus.PastDue,
                GraceDays = SubscriptionStatusCalculator.PastDueGracePeriodDays,
                Now = now
            },
            session.Transaction);

        // Trials past their end date are marked Expired (enforcement falls back to FREE automatically).
        changed += session.Connection.Execute(
            """
            UPDATE dbo.TenantSubscriptions
            SET Status = @ExpiredStatus, ModifiedAt = @Now
            WHERE Status = @TrialingStatus AND TrialEndsAt IS NOT NULL AND TrialEndsAt < @Now
            """,
            new { ExpiredStatus = (int)SubscriptionStatus.Expired, TrialingStatus = (int)SubscriptionStatus.Trialing, Now = now },
            session.Transaction);

        // Monthly usage counters are scoped per (TenantId, CounterCode, PeriodYear, PeriodMonth), so a new
        // calendar month naturally starts at zero without any explicit reset step.

        session.Commit();
        return changed;
    }

    private static DateTime FarFutureEnd(DateTime now) => now.AddYears(100);

    private static DateTime ResolvePeriodEnd(DateTime now, string packageCode, BillingCycle billingCycle, int? durationDays)
    {
        if (durationDays is { } days && days > 0)
        {
            return now.AddDays(days);
        }

        if (packageCode == PackageCode.Free)
        {
            return FarFutureEnd(now);
        }

        return billingCycle == BillingCycle.Yearly ? now.AddYears(1) : now.AddMonths(1);
    }

    private string BuildAbsoluteUrl(string path)
    {
        if (string.IsNullOrWhiteSpace(_settings.WebhookBaseUrl) || path.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || path.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return path;
        }

        return $"{_settings.WebhookBaseUrl.TrimEnd('/')}{path}";
    }

    private static string AppendQuery(string url, string key, string value)
    {
        var separator = url.Contains('?') ? "&" : "?";
        return $"{url}{separator}{key}={Uri.EscapeDataString(value)}";
    }

    private static bool TenantExists(IDbConnection conn, int tenantId, IDbTransaction? transaction) =>
        conn.ExecuteScalar<int>("SELECT COUNT(1) FROM dbo.Tenants WHERE Id = @TenantId", new { TenantId = tenantId }, transaction) > 0;

    private static void UpsertSubscription(
        DbSession session,
        int tenantId,
        int packageId,
        SubscriptionStatus status,
        BillingCycle billingCycle,
        DateTime periodStart,
        DateTime periodEnd,
        int? userId,
        DateTime? trialEndsAt)
    {
        var now = DateTime.UtcNow;
        var existingId = session.Connection.ExecuteScalar<int?>(
            "SELECT Id FROM dbo.TenantSubscriptions WHERE TenantId = @TenantId", new { TenantId = tenantId }, session.Transaction);

        if (existingId is { } id)
        {
            session.Connection.Execute(
                """
                UPDATE dbo.TenantSubscriptions
                SET PackageId = @PackageId, Status = @Status, BillingCycle = @BillingCycle,
                    CurrentPeriodStart = @PeriodStart, CurrentPeriodEnd = @PeriodEnd,
                    TrialEndsAt = @TrialEndsAt, CancelledAt = NULL, CancelAtPeriodEnd = 0,
                    ModifiedAt = @Now, ModifiedByUserId = @UserId
                WHERE Id = @Id
                """,
                new
                {
                    Id = id,
                    PackageId = packageId,
                    Status = (int)status,
                    BillingCycle = (int)billingCycle,
                    PeriodStart = periodStart,
                    PeriodEnd = periodEnd,
                    TrialEndsAt = trialEndsAt,
                    Now = now,
                    UserId = userId
                },
                session.Transaction);
        }
        else
        {
            session.Connection.Execute(
                """
                INSERT INTO dbo.TenantSubscriptions (TenantId, PackageId, Status, BillingCycle, CurrentPeriodStart, CurrentPeriodEnd, TrialEndsAt, CreatedAt, CreatedByUserId)
                VALUES (@TenantId, @PackageId, @Status, @BillingCycle, @PeriodStart, @PeriodEnd, @TrialEndsAt, @Now, @UserId)
                """,
                new
                {
                    TenantId = tenantId,
                    PackageId = packageId,
                    Status = (int)status,
                    BillingCycle = (int)billingCycle,
                    PeriodStart = periodStart,
                    PeriodEnd = periodEnd,
                    TrialEndsAt = trialEndsAt,
                    Now = now,
                    UserId = userId
                },
                session.Transaction);
        }
    }

    private const string SubscriptionRowSelect = """
        SELECT ts.Id, ts.TenantId, t.Name AS TenantName, t.Code AS TenantCode,
               ts.PackageId, pp.Code AS PackageCode, pp.Name AS PackageName, pp.SortOrder AS PackageSortOrder,
               ts.Status, ts.BillingCycle, ts.CurrentPeriodStart, ts.CurrentPeriodEnd, ts.TrialEndsAt,
               ts.CancelledAt, ts.CancelAtPeriodEnd, ts.ProviderCode, ts.ProviderSubscriptionId
        FROM dbo.TenantSubscriptions ts
        INNER JOIN dbo.PricingPackages pp ON pp.Id = ts.PackageId
        INNER JOIN dbo.Tenants t ON t.Id = ts.TenantId
        """;

    private static SubscriptionRow? LoadSubscriptionRow(IDbConnection conn, int tenantId, IDbTransaction? transaction)
    {
        return conn.QuerySingleOrDefault<SubscriptionRow>(
            SubscriptionRowSelect + " WHERE ts.TenantId = @TenantId",
            new { TenantId = tenantId },
            transaction);
    }

    private static SubscriptionPricingPackageRow GetPackageByCode(IDbConnection conn, string code, IDbTransaction? transaction)
    {
        var package = conn.QuerySingleOrDefault<SubscriptionPricingPackageRow>(
            "SELECT Id, Code, Name, MonthlyPrice, YearlyPrice, Currency, SortOrder, IsActive, IsCustomPricing FROM dbo.PricingPackages WHERE Code = @Code",
            new { Code = code },
            transaction);

        if (package is null)
        {
            throw new NotFoundException($"Pricing package '{code}' was not found.");
        }

        return package;
    }

    private static TenantSubscriptionDto BuildDto(IDbConnection conn, SubscriptionRow row, IDbTransaction? transaction)
    {
        var now = DateTime.UtcNow;
        var status = (SubscriptionStatus)row.Status;
        var effectiveStatus = SubscriptionStatusCalculator.ResolveEffectiveStatus(status, row.CurrentPeriodEnd, row.TrialEndsAt, now);

        var features = conn.Query<PackageFeature>(
            "SELECT * FROM dbo.PackageFeatures WHERE PackageId = @PackageId", new { row.PackageId }, transaction).ToList();
        var limits = conn.Query<PackageLimit>(
            "SELECT * FROM dbo.PackageLimits WHERE PackageId = @PackageId", new { row.PackageId }, transaction).ToList();

        var usage = new List<UsageCounterDto>();
        foreach (var counterCode in LimitCode.MonthlyCounters)
        {
            var limitValue = limits.FirstOrDefault(l => l.LimitCode == counterCode)?.LimitValue ?? 0;
            var currentValue = conn.ExecuteScalar<int?>(
                """
                SELECT CurrentValue FROM dbo.TenantUsageCounters
                WHERE TenantId = @TenantId AND CounterCode = @CounterCode AND PeriodYear = @Year AND PeriodMonth = @Month
                """,
                new { row.TenantId, CounterCode = counterCode, Year = now.Year, Month = now.Month },
                transaction) ?? 0;

            usage.Add(new UsageCounterDto { CounterCode = counterCode, CurrentValue = currentValue, LimitValue = limitValue });
        }

        return new TenantSubscriptionDto
        {
            Id = row.Id,
            TenantId = row.TenantId,
            TenantName = row.TenantName,
            TenantCode = row.TenantCode,
            PackageId = row.PackageId,
            PackageCode = row.PackageCode,
            PackageName = row.PackageName,
            Status = effectiveStatus.ToString(),
            BillingCycle = ((BillingCycle)row.BillingCycle).ToString(),
            CurrentPeriodStart = row.CurrentPeriodStart,
            CurrentPeriodEnd = row.CurrentPeriodEnd,
            TrialEndsAt = row.TrialEndsAt,
            CancelledAt = row.CancelledAt,
            CancelAtPeriodEnd = row.CancelAtPeriodEnd,
            Features = features.Select(f => new PackageFeatureDto { FeatureCode = f.FeatureCode, IsEnabled = f.IsEnabled }).ToList(),
            Limits = limits.Select(l => new PackageLimitDto { LimitCode = l.LimitCode, LimitValue = l.LimitValue }).ToList(),
            Usage = usage
        };
    }
}
