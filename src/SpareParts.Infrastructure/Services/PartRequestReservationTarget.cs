using SpareParts.Domain.Inventory;

namespace SpareParts.Infrastructure.Services;

/// <summary>Locked part request row used by <see cref="PartRequestsService"/> while starting a reservation clock.</summary>
internal sealed class PartRequestReservationTarget
{
    public int Id { get; set; }
    public int? PartId { get; set; }
    public int Quantity { get; set; }
    public string Status { get; set; } = PartRequestStatus.Open;
}
