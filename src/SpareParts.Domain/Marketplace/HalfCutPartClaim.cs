using SpareParts.Domain.Common;

namespace SpareParts.Domain.Marketplace
{
    public class HalfCutPartClaim : AuditableEntity
    {
        public int HalfCutListingId { get; set; }
        public int TenantId { get; set; }
        public int? ClaimantUserId { get; set; }
        public string ClaimantName { get; set; } = string.Empty;
        public string? ClaimantPhone { get; set; }
        public string PartName { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public decimal? DepositAmount { get; set; }
        public string Currency { get; set; } = "USD";
        public bool IsConfirmed { get; set; } = false;
    }
}
