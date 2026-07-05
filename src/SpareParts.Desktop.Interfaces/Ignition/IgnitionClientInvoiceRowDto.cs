using System;

namespace SpareParts.Desktop.Wpf.Interfaces
{
    /// <summary>
    /// Row of GET /api/clients/{id}/invoices (Report 04 §06). The route reuses the existing
    /// invoice service shape (see SpareParts.Domain.Pricing.InvoiceDto) with a customerId filter;
    /// tenant/subscription identifiers are intentionally not modeled client-side (no tenant leakage).
    /// </summary>
    public sealed class IgnitionClientInvoiceRowDto
    {
        public int Id { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public decimal Subtotal { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public string Currency { get; set; } = "USD";
        public string Status { get; set; } = string.Empty;
        public DateTime IssuedAt { get; set; }
        public DateTime? DueAt { get; set; }
        public DateTime? PaidAt { get; set; }
    }
}
