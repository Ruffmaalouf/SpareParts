namespace SpareParts.Domain.Marketplace
{
    public class EscrowTransactionDto
    {
        public int Id { get; set; }
        public int TenantId { get; set; }
        public int? SalesInvoiceId { get; set; }
        public int? PartId { get; set; }
        public string BuyerName { get; set; } = string.Empty;
        public string? BuyerPhone { get; set; }
        public string SellerName { get; set; } = string.Empty;
        public string? SellerPhone { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "USD";
        public EscrowStatus Status { get; set; } = EscrowStatus.PendingPayment;
        public string? Notes { get; set; }
        public string? TrackingNumber { get; set; }
        public DateTime? ShippedAt { get; set; }
        public DateTime? DeliveredAt { get; set; }
        public DateTime? ReleasedAt { get; set; }
        public DateTime? DisputedAt { get; set; }
        public string? DisputeReason { get; set; }
        public int AutoReleaseAfterHours { get; set; } = 48;
        public DateTime CreatedAt { get; set; }
        public string StatusLabel { get; set; } = string.Empty;
    }
}
