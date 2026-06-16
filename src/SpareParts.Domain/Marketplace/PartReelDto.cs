namespace SpareParts.Domain.Marketplace
{
    public class PartReelDto
    {
        public int Id { get; set; }
        public int TenantId { get; set; }
        public int? PartId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string VideoUrl { get; set; } = string.Empty;
        public string? ThumbnailUrl { get; set; }
        public string? PartName { get; set; }
        public decimal? Price { get; set; }
        public string Currency { get; set; } = "USD";
        public PartReelStatus Status { get; set; } = PartReelStatus.Active;
        public int ViewCount { get; set; } = 0;
        public int LikeCount { get; set; } = 0;
        public DateTime CreatedAt { get; set; }
        public string? SellerName { get; set; }
    }
}
