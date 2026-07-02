namespace SpareParts.Infrastructure.Services;

/// <summary>Minimal customer projection used by <see cref="UsedCarsService"/> when recording a used-car wholesale sale.</summary>
internal sealed class UsedCarWholesaleCustomerLookup
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
}
