namespace SpareParts.Infrastructure.Services;

/// <summary>Minimal customer projection used by <see cref="PartRequestsService"/> when creating a part request.</summary>
internal sealed class PartRequestCustomerLookup
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
}
