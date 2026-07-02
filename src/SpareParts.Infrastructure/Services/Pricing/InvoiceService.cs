using Dapper;
using SpareParts.Domain.Pricing;
using SpareParts.Infrastructure.Data;
using SpareParts.Infrastructure.Interfaces;

namespace SpareParts.Infrastructure.Services.Pricing;

public sealed class InvoiceService : IInvoiceService
{
    private readonly ISqlConnectionFactory _factory;
    private readonly PaymentSettings _settings;

    public InvoiceService(ISqlConnectionFactory factory, PaymentSettings settings)
    {
        _factory = factory;
        _settings = settings;
    }

    public IReadOnlyList<InvoiceDto> GetAll(int tenantId)
    {
        using var conn = _factory.CreateConnection();
        return conn.Query<InvoiceRow>(
                "SELECT * FROM dbo.Invoices WHERE TenantId = @TenantId ORDER BY IssuedAt DESC",
                new { TenantId = tenantId })
            .Select(MapToDto)
            .ToList();
    }

    public InvoiceDto GetById(int tenantId, int id)
    {
        using var conn = _factory.CreateConnection();
        var row = conn.QuerySingleOrDefault<InvoiceRow>(
            "SELECT * FROM dbo.Invoices WHERE Id = @Id AND TenantId = @TenantId",
            new { Id = id, TenantId = tenantId });

        if (row is null)
        {
            throw new NotFoundException($"Invoice {id} was not found.");
        }

        return MapToDto(row);
    }

    public int CreateForPayment(int paymentId, int? userId)
    {
        using var session = new DbSession(_factory);

        var existingId = session.Connection.ExecuteScalar<int?>(
            "SELECT Id FROM dbo.Invoices WHERE PaymentId = @PaymentId",
            new { PaymentId = paymentId },
            session.Transaction);
        if (existingId is { } existing)
        {
            session.Commit();
            return existing;
        }

        var payment = session.Connection.QuerySingleOrDefault<InvoicePaymentRow>(
            "SELECT * FROM dbo.Payments WHERE Id = @Id",
            new { Id = paymentId },
            session.Transaction);
        if (payment is null)
        {
            throw new NotFoundException($"Payment {paymentId} was not found.");
        }

        var now = DateTime.UtcNow;
        var subtotal = payment.Amount;
        var taxAmount = _settings.TaxEnabled ? Math.Round(subtotal * _settings.TaxPercentage / 100m, 2) : 0m;
        var totalAmount = subtotal + taxAmount;
        var status = payment.Status == (int)PaymentStatus.Succeeded ? InvoiceStatus.Paid : InvoiceStatus.Issued;

        var invoiceId = session.Connection.ExecuteScalar<int>(
            """
            INSERT INTO dbo.Invoices (TenantId, SubscriptionId, PaymentId, InvoiceNumber, Subtotal, TaxAmount, TotalAmount, Currency, Status, IssuedAt, PaidAt, CreatedAt, CreatedByUserId)
            VALUES (@TenantId, @SubscriptionId, @PaymentId, @TempNumber, @Subtotal, @TaxAmount, @TotalAmount, @Currency, @Status, @Now, @PaidAt, @Now, @UserId);
            SELECT CAST(SCOPE_IDENTITY() AS INT);
            """,
            new
            {
                payment.TenantId,
                payment.SubscriptionId,
                PaymentId = paymentId,
                TempNumber = $"TEMP-{Guid.NewGuid():N}",
                Subtotal = subtotal,
                TaxAmount = taxAmount,
                TotalAmount = totalAmount,
                payment.Currency,
                Status = (int)status,
                Now = now,
                payment.PaidAt,
                UserId = userId
            },
            session.Transaction);

        session.Connection.Execute(
            "UPDATE dbo.Invoices SET InvoiceNumber = @InvoiceNumber WHERE Id = @Id",
            new { Id = invoiceId, InvoiceNumber = $"INV-{now:yyyy}-{invoiceId:D6}" },
            session.Transaction);

        session.Commit();
        return invoiceId;
    }

    public IReadOnlyList<InvoiceDto> GetAllForAdmin()
    {
        using var conn = _factory.CreateConnection();
        return conn.Query<InvoiceRow>("SELECT * FROM dbo.Invoices ORDER BY IssuedAt DESC")
            .Select(MapToDto)
            .ToList();
    }

    private static InvoiceDto MapToDto(InvoiceRow row) => new()
    {
        Id = row.Id,
        InvoiceNumber = row.InvoiceNumber,
        TenantId = row.TenantId,
        SubscriptionId = row.SubscriptionId,
        PaymentId = row.PaymentId,
        Subtotal = row.Subtotal,
        TaxAmount = row.TaxAmount,
        TotalAmount = row.TotalAmount,
        Currency = row.Currency,
        Status = ((InvoiceStatus)row.Status).ToString(),
        IssuedAt = row.IssuedAt,
        DueAt = row.DueAt,
        PaidAt = row.PaidAt
    };
}
