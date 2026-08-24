namespace SpareParts.Domain.Clients
{
    /// <summary>One part request row for the workspace Requests tab (Report 04 §06).</summary>
    public sealed class ClientPartRequestDto
    {
        public int RequestId { get; set; }
        public string RequestedPartName { get; set; } = string.Empty;
        public string? RequestedOemNumber { get; set; }
        public string? VehicleDetails { get; set; }
        public int Quantity { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
