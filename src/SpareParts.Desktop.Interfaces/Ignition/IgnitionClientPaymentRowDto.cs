using System;

namespace SpareParts.Desktop.Wpf.Interfaces
{
    /// <summary>
    /// Row of GET /api/clients/{id}/payments (Report 04 §06). The route reuses the existing
    /// payment history shape (see SpareParts.Domain.Pricing.PaymentDto) with a customerId filter;
    /// tenant/provider internals are intentionally not modeled client-side.
    /// </summary>
    public sealed class IgnitionClientPaymentRowDto
    {
        public int Id { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "USD";
        public string Status { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public DateTime? PaidAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
