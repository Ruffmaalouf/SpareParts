namespace SpareParts.Domain.Inventory
{
    public sealed class UpdatePartRequestStatusRequest
    {
        public string Status { get; set; } = PartRequestStatus.Open;
    }
}
