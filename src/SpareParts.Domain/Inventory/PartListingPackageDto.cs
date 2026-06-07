using SpareParts.Domain.Cars;

namespace SpareParts.Domain.Inventory
{
    public sealed class PartListingPackageDto
    {
        public int PartId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string PriceText { get; set; } = string.Empty;
        public int PhotoCount { get; set; }
        public List<MarketplaceLinkDto> MarketplaceLinks { get; set; } = [];
    }
}
