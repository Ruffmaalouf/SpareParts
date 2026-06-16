namespace SpareParts.Domain.Referral
{
    public class ReferralSignupDto
    {
        public int Id { get; set; }
        public int ReferralCodeId { get; set; }
        public int? ReferredTenantId { get; set; }
        public int? ReferredUserId { get; set; }
        public string ReferredEmail { get; set; } = string.Empty;
        public decimal CreditAmount { get; set; } = 0;
        public string Currency { get; set; } = "USD";
        public bool CreditPaid { get; set; } = false;
        public DateTime? CreditPaidAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? ReferredName { get; set; }
    }
}
