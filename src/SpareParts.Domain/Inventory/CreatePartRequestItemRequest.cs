namespace SpareParts.Domain.Inventory
{
    public sealed class CreatePartRequestItemRequest
    {
        public int? PartId { get; set; }
        public int? CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string? CustomerPhone { get; set; }
        public string RequestedPartName { get; set; } = string.Empty;
        public string? RequestedOemNumber { get; set; }
        public string? VehicleDetails { get; set; }
        public int Quantity { get; set; } = 1;
        public string? Notes { get; set; }
    }
}
