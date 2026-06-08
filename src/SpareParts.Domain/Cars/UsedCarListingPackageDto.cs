using SpareParts.Domain.Inventory;

namespace SpareParts.Domain.Cars
{
    public sealed class UsedCarListingPackageDto
    {
        public int UsedCarId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string PriceText { get; set; } = string.Empty;
        public int PhotoCount { get; set; }
        public List<MarketplaceLinkDto> MarketplaceLinks { get; set; } = [];
    }

   
}
