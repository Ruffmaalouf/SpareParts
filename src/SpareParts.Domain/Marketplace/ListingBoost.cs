using SpareParts.Domain.Common;

namespace SpareParts.Domain.Marketplace
{
    public class ListingBoost : AuditableEntity
    {
        public int TenantId { get; set; }
        public int PartId { get; set; }
        public int DurationDays { get; set; }
        public decimal? Fee { get; set; }
        public string Currency { get; set; } = "USD";
        public ListingBoostStatus Status { get; set; } = ListingBoostStatus.Active;
        public DateTime StartsAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public string? PaymentReference { get; set; }
    }
}
