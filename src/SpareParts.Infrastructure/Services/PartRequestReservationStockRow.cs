namespace SpareParts.Infrastructure.Services;

/// <summary>Available stock row used by <see cref="PartRequestsService"/> when allocating a part request reservation.</summary>
internal sealed class PartRequestReservationStockRow
{
    public int Id { get; set; }
    public int WarehouseId { get; set; }
    public int AvailableQuantity { get; set; }
}
