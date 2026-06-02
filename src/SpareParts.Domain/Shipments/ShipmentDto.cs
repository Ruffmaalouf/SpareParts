using System.ComponentModel.DataAnnotations;

namespace SpareParts.Domain.Shipments;

public class ShipmentDto
{
    public int Id { get; set; }
    public string ShipmentNumber { get; set; } = string.Empty;
    public int? SalesInvoiceId { get; set; }
    public string? InvoiceNumber { get; set; }
    public int? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? CarrierName { get; set; }
    public string? TrackingNumber { get; set; }
    public string? DeliveryAddress { get; set; }
    public DateTime? EstimatedDelivery { get; set; }
    public DateTime? ActualDelivery { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<ShipmentEventDto> Events { get; set; } = new();
}

public class ShipmentEventDto
{
    public int Id { get; set; }
    public int ShipmentId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Location { get; set; }
    public string? Notes { get; set; }
    public DateTime EventAt { get; set; }
}

public class CreateShipmentRequest
{
    public int? SalesInvoiceId { get; set; }
    public int? CustomerId { get; set; }
    [MaxLength(200)] public string? CarrierName { get; set; }
    [MaxLength(100)] public string? TrackingNumber { get; set; }
    [MaxLength(500)] public string? DeliveryAddress { get; set; }
    public DateTime? EstimatedDelivery { get; set; }
    [MaxLength(1000)] public string? Notes { get; set; }
}

public class UpdateShipmentStatusRequest
{
    [MaxLength(50)] public string Status { get; set; } = "InTransit";
    [MaxLength(200)] public string? Location { get; set; }
    [MaxLength(1000)] public string? Notes { get; set; }
    public DateTime? ActualDelivery { get; set; }
}
