using System.ComponentModel.DataAnnotations;

namespace SpareParts.Domain.WebCatalog
{
    public sealed class WebCatalogPartDto
    {
        public int Id { get; set; }
        public string InternalCode { get; set; } = string.Empty;
        public string? Barcode { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? OEMNumber { get; set; }
        public string Condition { get; set; } = string.Empty;
        public decimal SalePrice { get; set; }
        public string Currency { get; set; } = "USD";
        public string? Notes { get; set; }
        public int AvailableQuantity { get; set; }
        public int WarehouseId { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
    }

    public sealed class WebCheckoutItemDto
    {
        public int PartId { get; set; }

        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }
    }

    public sealed class WebCheckoutRequest
    {
        [MaxLength(160)]
        public string? CustomerName { get; set; }

        [MaxLength(80)]
        public string? CustomerPhone { get; set; }

        [MaxLength(240)]
        public string? CustomerEmail { get; set; }

        [MaxLength(200)]
        public string? ShippingAddressLine1 { get; set; }

        [MaxLength(200)]
        public string? ShippingAddressLine2 { get; set; }

        [MaxLength(120)]
        public string? ShippingCity { get; set; }

        [MaxLength(120)]
        public string? ShippingRegion { get; set; }

        [MaxLength(40)]
        public string? ShippingPostalCode { get; set; }

        [MaxLength(120)]
        public string? ShippingCountry { get; set; }

        [MaxLength(500)]
        public string? DeliveryInstructions { get; set; }

        [MaxLength(40)]
        public string? PaymentMethod { get; set; }

        [MaxLength(120)]
        public string? PaymentReference { get; set; }

        [MinLength(1)]
        public List<WebCheckoutItemDto> Items { get; set; } = new();
    }

    public sealed class WebCheckoutResponse
    {
        public int InvoiceId { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public string PaymentStatus { get; set; } = string.Empty;
        public int WarehouseId { get; set; }
    }
}
