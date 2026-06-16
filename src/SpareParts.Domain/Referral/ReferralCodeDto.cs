namespace SpareParts.Domain.Referral
{
    public class ReferralCodeDto
    {
        public int Id { get; set; }
        public int TenantId { get; set; }
        public int UserId { get; set; }
        public string Code { get; set; } = string.Empty;
        public int TotalReferrals { get; set; } = 0;
        public decimal TotalCreditsEarned { get; set; } = 0;
        public string Currency { get; set; } = "USD";
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public string ReferralUrl { get; set; } = string.Empty;
    }
}
