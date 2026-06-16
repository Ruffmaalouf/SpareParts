namespace SpareParts.Domain.Verification
{
    public class SellerVerificationDto
    {
        public int TenantId { get; set; }
        public SellerVerificationStatus Status { get; set; }
        public string? BusinessName { get; set; }
        public string? RegistrationNumber { get; set; }
        public bool PhoneVerified { get; set; }
        public string? IdDocumentUrl { get; set; }
        public string? BusinessDocumentUrl { get; set; }
        public string? PhysicalAddress { get; set; }
        public string? AdminNotes { get; set; }
        public DateTime? VerifiedAt { get; set; }
        public DateTime? SubmittedAt { get; set; }
    }
}
