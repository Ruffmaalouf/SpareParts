namespace SpareParts.Domain.Marketplace
{
    public class WhatsAppCatalogItemDto
    {
        public string ProductId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Currency { get; set; } = string.Empty;
        public string Availability { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public string? Barcode { get; set; }
        public string? Category { get; set; }
        public string ListingUrl { get; set; } = string.Empty;
    }
}
