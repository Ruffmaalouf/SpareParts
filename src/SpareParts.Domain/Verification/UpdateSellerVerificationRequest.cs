namespace SpareParts.Domain.Verification
{
    public class UpdateSellerVerificationRequest
    {
        public string? BusinessName { get; set; }
        public string? RegistrationNumber { get; set; }
        public string? PhysicalAddress { get; set; }
        public string? IdDocumentUrl { get; set; }
        public string? BusinessDocumentUrl { get; set; }
    }
}
