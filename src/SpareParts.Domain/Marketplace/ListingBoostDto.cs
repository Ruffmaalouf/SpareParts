namespace SpareParts.Domain.Marketplace
{
    public class ListingBoostDto
    {
        public int Id { get; set; }
        public int TenantId { get; set; }
        public int PartId { get; set; }
        public int DurationDays { get; set; }
        public decimal? Fee { get; set; }
        public string Currency { get; set; } = "USD";
        public ListingBoostStatus Status { get; set; } = ListingBoostStatus.Active;
        public string StatusLabel { get; set; } = string.Empty;
        public DateTime StartsAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public string? PaymentReference { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? PartName { get; set; }
        public string? PartInternalCode { get; set; }
        public bool IsCurrentlyActive { get; set; }
    }
}
