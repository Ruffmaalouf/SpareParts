using System.ComponentModel.DataAnnotations;

namespace SpareParts.Domain.Sales
{
    public sealed class IssueSalePaymentRequest
    {
        public DateTime PaymentDate { get; set; } = DateTime.Today;

        [Range(0.0001, double.MaxValue)]
        public decimal Amount { get; set; }

        [MaxLength(50)]
        public string? PaymentMethod { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }
    }
}
