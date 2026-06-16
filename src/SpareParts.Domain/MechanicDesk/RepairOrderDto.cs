namespace SpareParts.Domain.MechanicDesk
{
    public class RepairOrderDto
    {
        public int Id { get; set; }
        public int TenantId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string? CustomerPhone { get; set; }
        public string? VehicleMake { get; set; }
        public string? VehicleModel { get; set; }
        public int? VehicleYear { get; set; }
        public string? VinNumber { get; set; }
        public string? Notes { get; set; }
        public string? LocationCity { get; set; }
        public RepairOrderStatus Status { get; set; } = RepairOrderStatus.Draft;
        public DateTime? SentAt { get; set; }
        public DateTime? DeadlineAt { get; set; }
        public int ItemCount { get; set; }
        public int TotalQuotes { get; set; }
        public string StatusLabel { get; set; } = string.Empty;
        public List<RepairOrderItemDto> Items { get; set; } = new();
    }
}
