using Dapper;
using SpareParts.Domain.Pricing;
using SpareParts.Infrastructure.Data;
using SpareParts.Infrastructure.Interfaces;

namespace SpareParts.Infrastructure.Services.Pricing;

public sealed class PaymentService : IPaymentService
{
    /// <summary>Used for CreatedByUserId/ModifiedByUserId on subscription changes triggered by webhooks (no acting user).</summary>
    private const int SystemUserId = 0;

    private readonly ISqlConnectionFactory _factory;
    private readonly IPaymentProviderFactory _providerFactory;
    private readonly ISubscriptionService _subscriptionService;
    private readonly IInvoiceService _invoiceService;

    public PaymentService(
        ISqlConnectionFactory factory,
        IPaymentProviderFactory providerFactory,
        ISubscriptionService subscriptionService,
        IInvoiceService invoiceService)
    {
        _factory = factory;
        _providerFactory = providerFactory;
        _subscriptionService = subscriptionService;
        _invoiceService = invoiceService;
    }

    public IReadOnlyList<PaymentDto> GetHistory(int tenantId)
    {
        using var conn = _factory.CreateConnection();
        return conn.Query<PaymentRow>(
                "SELECT * FROM dbo.Payments WHERE TenantId = @TenantId ORDER BY CreatedAt DESC",
                new { TenantId = tenantId })
            .Select(MapToDto)
            .ToList();
    }

    public PaymentDto GetById(int tenantId, int id)
    {
        using var conn = _factory.CreateConnection();
        var row = conn.QuerySingleOrDefault<PaymentRow>(
            "SELECT * FROM dbo.Payments WHERE Id = @Id AND TenantId = @TenantId",
            new { Id = id, TenantId = tenantId });

        if (row is null)
        {
            throw new NotFoundException($"Payment {id} was not found.");
        }

        return MapToDto(row);
    }

    public async Task<WebhookResult> HandleWebhookAsync(string providerCode, WebhookRequestContext context, CancellationToken cancellationToken)
    {
        var provider = _providerFactory.GetProvider(providerCode);
        var result = await provider.HandleWebhookAsync(context, cancellationToken);

        var eventId = string.IsNullOrWhiteSpace(result.ProviderEventId)
            ? $"{provider.ProviderCode}-{Guid.NewGuid():N}"
            : result.ProviderEventId;
        var eventType = string.IsNullOrWhiteSpace(result.EventType) ? "unknown" : result.EventType;

        using var session = new DbSession(_factory);

        var existingStatus = session.Connection.ExecuteScalar<int?>(
            "SELECT Status FROM dbo.WebhookEvents WHERE Provider = @Provider AND EventId = @EventId",
            new { Provider = provider.ProviderCode, EventId = eventId },
            session.Transaction);

        if (existingStatus == (int)WebhookProcessingStatus.Processed)
        {
            session.Commit();
            return new WebhookResult { Success = true, Message = "Event already processed.", ProviderEventId = eventId, EventType = eventType };
        }

        if (!result.Success)
        {
            UpsertWebhookEvent(session, provider.ProviderCode, eventId, eventType, context.RawBody, WebhookProcessingStatus.Failed, result.Message);
            session.Commit();
            return result;
        }

        PaymentRow? payment = null;
        if (int.TryParse(result.ProviderPaymentId, out var paymentId))
        {
            payment = session.Connection.QuerySingleOrDefault<PaymentRow>(
                "SELECT * FROM dbo.Payments WHERE Id = @Id", new { Id = paymentId }, session.Transaction);
        }

        if (payment is null)
        {
            UpsertWebhookEvent(session, provider.ProviderCode, eventId, eventType, context.RawBody, WebhookProcessingStatus.Ignored, "No matching payment found.");
            session.Commit();
            return result;
        }

        if (result.PaymentSucceeded == true)
        {
            session.Connection.Execute(
                "UPDATE dbo.Payments SET Status = @Status, PaidAt = @Now, ProviderPaymentId = @ProviderPaymentId, ModifiedAt = @Now WHERE Id = @Id",
                new { Status = (int)PaymentStatus.Succeeded, Now = DateTime.UtcNow, result.ProviderPaymentId, Id = payment.Id },
                session.Transaction);
            PaymentTransactionLog.Insert(session.Connection, session.Transaction, payment.Id, provider.ProviderCode, PaymentTransactionStatus.Succeeded, $"Webhook event '{eventType}' confirmed payment.", context.RawBody);
        }
        else if (result.PaymentSucceeded == false)
        {
            session.Connection.Execute(
                "UPDATE dbo.Payments SET Status = @Status, ModifiedAt = @Now WHERE Id = @Id",
                new { Status = (int)PaymentStatus.Failed, Now = DateTime.UtcNow, Id = payment.Id },
                session.Transaction);
            PaymentTransactionLog.Insert(session.Connection, session.Transaction, payment.Id, provider.ProviderCode, PaymentTransactionStatus.Failed, $"Webhook event '{eventType}' reported payment failure.", context.RawBody);
        }
        else
        {
            PaymentTransactionLog.Insert(session.Connection, session.Transaction, payment.Id, provider.ProviderCode, PaymentTransactionStatus.Pending, $"Webhook event '{eventType}' received.", context.RawBody);
        }

        UpsertWebhookEvent(session, provider.ProviderCode, eventId, eventType, context.RawBody, WebhookProcessingStatus.Processed, null);
        session.Commit();

        if (result.PaymentSucceeded == true)
        {
            _subscriptionService.AdminActivate(new AdminActivateSubscriptionRequest
            {
                TenantId = payment.TenantId,
                PackageCode = payment.PackageCode,
                BillingCycle = ((BillingCycle)payment.BillingCycle).ToString(),
                PaymentId = payment.Id
            }, SystemUserId);

            _invoiceService.CreateForPayment(payment.Id, null);
        }

        return result;
    }

    public IReadOnlyList<PaymentDto> GetAllForAdmin()
    {
        using var conn = _factory.CreateConnection();
        return conn.Query<PaymentRow>("SELECT * FROM dbo.Payments ORDER BY CreatedAt DESC")
            .Select(MapToDto)
            .ToList();
    }

    public IReadOnlyList<WebhookEventDto> GetWebhookEventsForAdmin()
    {
        using var conn = _factory.CreateConnection();
        return conn.Query<WebhookEventRow>("SELECT * FROM dbo.WebhookEvents ORDER BY CreatedAt DESC")
            .Select(row => new WebhookEventDto
            {
                Id = row.Id,
                Provider = row.Provider,
                EventId = row.EventId,
                EventType = row.EventType,
                Status = ((WebhookProcessingStatus)row.Status).ToString(),
                ProcessedAt = row.ProcessedAt,
                Error = row.Error,
                CreatedAt = row.CreatedAt
            })
            .ToList();
    }

    public PaymentDto MarkPaid(int paymentId, MarkPaymentPaidRequest request, int userId)
    {
        using var session = new DbSession(_factory);
        var payment = session.Connection.QuerySingleOrDefault<PaymentRow>(
            "SELECT * FROM dbo.Payments WHERE Id = @Id", new { Id = paymentId }, session.Transaction);

        if (payment is null)
        {
            throw new NotFoundException($"Payment {paymentId} was not found.");
        }

        if (payment.Status == (int)PaymentStatus.Succeeded)
        {
            throw new ConflictException("This payment has already been marked as paid.");
        }

        var now = DateTime.UtcNow;
        session.Connection.Execute(
            """
            UPDATE dbo.Payments
            SET Status = @Status, PaidAt = @Now,
                ProviderPaymentId = COALESCE(@ProviderPaymentId, ProviderPaymentId),
                Notes = COALESCE(@Notes, Notes),
                ModifiedAt = @Now, ModifiedByUserId = @UserId
            WHERE Id = @Id
            """,
            new
            {
                Status = (int)PaymentStatus.Succeeded,
                Now = now,
                request.ProviderPaymentId,
                request.Notes,
                UserId = userId,
                Id = paymentId
            },
            session.Transaction);

        PaymentTransactionLog.Insert(session.Connection, session.Transaction, paymentId, payment.ProviderCode, PaymentTransactionStatus.Succeeded, "Marked as paid by admin.");

        var updated = session.Connection.QuerySingleOrDefault<PaymentRow>(
            "SELECT * FROM dbo.Payments WHERE Id = @Id", new { Id = paymentId }, session.Transaction)!;

        session.Commit();

        _subscriptionService.AdminActivate(new AdminActivateSubscriptionRequest
        {
            TenantId = payment.TenantId,
            PackageCode = payment.PackageCode,
            BillingCycle = ((BillingCycle)payment.BillingCycle).ToString(),
            PaymentId = paymentId
        }, userId);

        _invoiceService.CreateForPayment(paymentId, userId);

        return MapToDto(updated);
    }

    private static void UpsertWebhookEvent(DbSession session, string provider, string eventId, string eventType, string payload, WebhookProcessingStatus status, string? error)
    {
        var now = DateTime.UtcNow;
        var existingId = session.Connection.ExecuteScalar<int?>(
            "SELECT Id FROM dbo.WebhookEvents WHERE Provider = @Provider AND EventId = @EventId",
            new { Provider = provider, EventId = eventId }, session.Transaction);

        if (existingId is { } id)
        {
            session.Connection.Execute(
                "UPDATE dbo.WebhookEvents SET EventType = @EventType, Payload = @Payload, Status = @Status, ProcessedAt = @Now, Error = @Error, ModifiedAt = @Now WHERE Id = @Id",
                new { Id = id, EventType = eventType, Payload = payload, Status = (int)status, Now = now, Error = error },
                session.Transaction);
            return;
        }

        session.Connection.Execute(
            """
            INSERT INTO dbo.WebhookEvents (Provider, EventId, EventType, Payload, Status, ProcessedAt, Error, CreatedAt)
            VALUES (@Provider, @EventId, @EventType, @Payload, @Status, @Now, @Error, @Now)
            """,
            new { Provider = provider, EventId = eventId, EventType = eventType, Payload = payload, Status = (int)status, Now = now, Error = error },
            session.Transaction);
    }

    private static PaymentDto MapToDto(PaymentRow row) => new()
    {
        Id = row.Id,
        TenantId = row.TenantId,
        SubscriptionId = row.SubscriptionId,
        ProviderCode = row.ProviderCode,
        ProviderPaymentId = row.ProviderPaymentId,
        ProviderCheckoutSessionId = row.ProviderCheckoutSessionId,
        PackageCode = row.PackageCode,
        BillingCycle = ((BillingCycle)row.BillingCycle).ToString(),
        Amount = row.Amount,
        Currency = row.Currency,
        Status = ((PaymentStatus)row.Status).ToString(),
        Notes = row.Notes,
        PaidAt = row.PaidAt,
        CreatedAt = row.CreatedAt
    };

    private sealed class PaymentRow
    {
        public int Id { get; set; }
        public int TenantId { get; set; }
        public int? SubscriptionId { get; set; }
        public string ProviderCode { get; set; } = string.Empty;
        public string? ProviderPaymentId { get; set; }
        public string? ProviderCheckoutSessionId { get; set; }
        public string PackageCode { get; set; } = string.Empty;
        public int BillingCycle { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "USD";
        public int Status { get; set; }
        public string? Notes { get; set; }
        public DateTime? PaidAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    private sealed class WebhookEventRow
    {
        public int Id { get; set; }
        public string Provider { get; set; } = string.Empty;
        public string EventId { get; set; } = string.Empty;
        public string EventType { get; set; } = string.Empty;
        public int Status { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public string? Error { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
