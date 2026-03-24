using System.ComponentModel.DataAnnotations;

namespace SpareParts.Domain.Sales
{
    public class CreateSaleRequest
    {
        public DateTime InvoiceDate { get; set; }
        public int? CustomerId { get; set; }
        [Range(1, int.MaxValue)]
        public int WarehouseId { get; set; }
        [MaxLength(50)]
        public string? PaymentMethod { get; set; }
        [Range(0, double.MaxValue)]
        public decimal PaidAmount { get; set; }
        [MaxLength(1000)]
        public string? Notes { get; set; }
        [MinLength(1)]
        public List<SaleItemDto> Items { get; set; } = new();
    }
}
