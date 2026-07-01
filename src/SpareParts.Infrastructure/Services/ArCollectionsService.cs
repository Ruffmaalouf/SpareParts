using Dapper;
using SpareParts.Domain.Communications;
using SpareParts.Domain.Sales;
using SpareParts.Infrastructure.Interfaces;

namespace SpareParts.Infrastructure.Services;

/// <summary>
/// Backs the AR Collections Agent. Finds unpaid sales invoices overdue by more than a grace
/// period and sends the customer a WhatsApp payment reminder, reusing the existing
/// <see cref="CommunicationTemplateKey.PaymentReminder"/> template — which already resolves the
/// customer's name/phone straight from the invoice, so this only needs to supply the invoice id.
/// Each invoice is reminded at most once per cooldown window, not every single day.
/// </summary>
public sealed class ArCollectionsService
{
    private const int OverdueGraceDays = 30;
    private const int ReminderCooldownDays = 7;

    private readonly ISqlConnectionFactory _factory;
    private readonly ITenantContext _tenantContext;
    private readonly CommunicationsService _communicationsService;

    public ArCollectionsService(
        ISqlConnectionFactory factory,
        ITenantContext tenantContext,
        CommunicationsService communicationsService)
    {
        _factory = factory;
        _tenantContext = tenantContext;
        _communicationsService = communicationsService;
    }

    public IReadOnlyList<OverdueInvoiceDto> GetOverdueInvoices()
    {
        var tenantId = _tenantContext.TenantId;
        using var conn = _factory.CreateConnection();
        return conn.Query<OverdueInvoiceDto>(
            @"SELECT t.Id AS TransactionId,
                     t.ReferenceId AS InvoiceId,
                     c.Name AS CustomerName,
                     c.Phone AS CustomerPhone,
                     (t.TotalAmount - t.PaidAmount) AS Balance,
                     t.TransactionDate,
                     ISNULL(t.CreatedByUserId, 0) AS CreatedByUserId
              FROM dbo.Transactions t
              INNER JOIN dbo.TransactionTypes tt ON tt.Id = t.TransactionTypeId AND tt.TypeKey = 'Sale'
              INNER JOIN dbo.Customers c ON c.Id = t.CustomerId
              WHERE t.CustomerId IS NOT NULL
                AND ISNULL(t.IsReturn, 0) = 0
                AND (t.TotalAmount - t.PaidAmount) > 0
                AND t.TransactionDate < DATEADD(DAY, -@OverdueGraceDays, SYSUTCDATETIME())
                AND c.Phone IS NOT NULL
                AND LTRIM(RTRIM(c.Phone)) <> ''
                AND (t.LastPaymentReminderSentAt IS NULL OR t.LastPaymentReminderSentAt < DATEADD(DAY, -@ReminderCooldownDays, SYSUTCDATETIME()))
                AND (@TenantId = 0 OR t.TenantId = @TenantId)
              ORDER BY t.TransactionDate ASC;",
            new { TenantId = tenantId, OverdueGraceDays, ReminderCooldownDays }).ToList();
    }

    public async Task SendReminderAsync(OverdueInvoiceDto invoice, CancellationToken cancellationToken)
    {
        var request = new SendBusinessMessageRequest
        {
            Channel = CommunicationChannel.WhatsApp,
            TemplateKey = CommunicationTemplateKey.PaymentReminder,
            SalesInvoiceId = invoice.InvoiceId
        };

        await _communicationsService.SendAsync(request, invoice.CreatedByUserId, cancellationToken);
    }

    public void MarkReminderSent(int transactionId)
    {
        using var conn = _factory.CreateConnection();
        conn.Execute(
            "UPDATE dbo.Transactions SET LastPaymentReminderSentAt = SYSUTCDATETIME() WHERE Id = @TransactionId;",
            new { TransactionId = transactionId });
    }
}
