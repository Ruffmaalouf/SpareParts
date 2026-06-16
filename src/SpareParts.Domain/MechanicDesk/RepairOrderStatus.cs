namespace SpareParts.Domain.MechanicDesk
{
    public enum RepairOrderStatus
    {
        Draft = 1,
        Sent = 2,
        QuotesReceived = 3,
        PartiallyAccepted = 4,
        Accepted = 5,
        Cancelled = 6
    }
}
