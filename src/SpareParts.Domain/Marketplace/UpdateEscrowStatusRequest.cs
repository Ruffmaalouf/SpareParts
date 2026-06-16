using System.ComponentModel.DataAnnotations;

namespace SpareParts.Domain.Marketplace
{
    public class UpdateEscrowStatusRequest
    {
        [Required]
        public EscrowStatus NewStatus { get; set; }

        public string? Notes { get; set; }

        public string? TrackingNumber { get; set; }

        public string? DisputeReason { get; set; }
    }
}
