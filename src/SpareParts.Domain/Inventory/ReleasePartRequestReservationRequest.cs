namespace SpareParts.Domain.Inventory
{
    public sealed class ReleasePartRequestReservationRequest
    {
        public string? Reason { get; set; }
        public string? NextStatus { get; set; }
    }
}
