namespace SpareParts.Infrastructure.Services.Pricing;

/// <summary>Raw <c>dbo.Invoices</c> row used by <see cref="InvoiceService"/>.</summary>
internal sealed class InvoiceRow
{
    public int Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public int TenantId { get; set; }
    public int? SubscriptionId { get; set; }
    public int? PaymentId { get; set; }
    public decimal Subtotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "USD";
    public int Status { get; set; }
    public DateTime IssuedAt { get; set; }
    public DateTime? DueAt { get; set; }
    public DateTime? PaidAt { get; set; }
}
