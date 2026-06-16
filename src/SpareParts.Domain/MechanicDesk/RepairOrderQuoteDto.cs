namespace SpareParts.Domain.MechanicDesk
{
    public class RepairOrderQuoteDto
    {
        public int Id { get; set; }
        public int RepairOrderItemId { get; set; }
        public int TenantId { get; set; }
        public string SellerName { get; set; } = string.Empty;
        public string? SellerPhone { get; set; }
        public int? PartId { get; set; }
        public decimal OfferedPrice { get; set; }
        public string Currency { get; set; } = "USD";
        public string Condition { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public bool IsAccepted { get; set; } = false;
        public DateTime? ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
