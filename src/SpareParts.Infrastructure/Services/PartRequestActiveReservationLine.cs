namespace SpareParts.Infrastructure.Services;

/// <summary>Active reservation line used by <see cref="PartRequestsService"/> when releasing reserved stock back to availability.</summary>
internal sealed class PartRequestActiveReservationLine
{
    public int Id { get; set; }
    public int StockId { get; set; }
    public int Quantity { get; set; }
}
