namespace SpareParts.Infrastructure.Services.Pricing;

/// <summary>
/// Minimal <c>dbo.Payments</c> projection used by <see cref="InvoiceService.CreateForPayment"/> to
/// compute invoice totals. Distinct from <see cref="PaymentRow"/> (used by <see cref="PaymentService"/>),
/// which projects the full payment record.
/// </summary>
internal sealed class InvoicePaymentRow
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public int? SubscriptionId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public int Status { get; set; }
    public DateTime? PaidAt { get; set; }
}
