using SpareParts.Domain.Common;

namespace SpareParts.Domain.Referral
{
    public class ReferralCode : AuditableEntity
    {
        public int TenantId { get; set; }
        public int UserId { get; set; }
        public string Code { get; set; } = string.Empty;
        public int TotalReferrals { get; set; } = 0;
        public decimal TotalCreditsEarned { get; set; } = 0;
        public string Currency { get; set; } = "USD";
        public bool IsActive { get; set; } = true;
    }
}
