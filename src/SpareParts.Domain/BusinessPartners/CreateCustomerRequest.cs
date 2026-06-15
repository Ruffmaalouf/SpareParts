using System.ComponentModel.DataAnnotations;

namespace SpareParts.Domain.BusinessPartners
{
    public sealed class CreateCustomerRequest
    {
        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(30)]
        public string? Phone { get; set; }

        [EmailAddress, MaxLength(200)]
        public string? Email { get; set; }

        [MaxLength(500)]
        public string? Address { get; set; }

        [MaxLength(50)]
        public string? TaxNumber { get; set; }

        [Range(0, double.MaxValue)]
        public decimal OpeningBalance { get; set; }

        [Range(0, double.MaxValue)]
        public decimal CreditLimit { get; set; }
    }
}
