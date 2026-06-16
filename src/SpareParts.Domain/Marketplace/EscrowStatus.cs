namespace SpareParts.Domain.Marketplace
{
    public enum EscrowStatus
    {
        PendingPayment = 1,
        Held = 2,
        Shipped = 3,
        Delivered = 4,
        BuyerAccepted = 5,
        Released = 6,
        Disputed = 7,
        Refunded = 8,
        Cancelled = 9
    }
}
