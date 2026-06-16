using SpareParts.Domain.Common;

namespace SpareParts.Domain.NeedBoard
{
    public class PartWantedAdOffer : AuditableEntity
    {
        public int WantedAdId { get; set; }
        public int TenantId { get; set; }
        public int? SellerUserId { get; set; }
        public string SellerName { get; set; } = string.Empty;
        public string? SellerPhone { get; set; }
        public int? PartId { get; set; }
        public decimal? OfferedPrice { get; set; }
        public string Currency { get; set; } = "USD";
        public string? Condition { get; set; }
        public string? Notes { get; set; }
        public string? PhotoUrl { get; set; }
        public bool IsAccepted { get; set; } = false;
    }
}
