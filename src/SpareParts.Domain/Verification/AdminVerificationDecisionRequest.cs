namespace SpareParts.Domain.Verification
{
    public class AdminVerificationDecisionRequest
    {
        public int TenantId { get; set; }
        public SellerVerificationStatus Decision { get; set; }
        public string? AdminNotes { get; set; }
    }
}
