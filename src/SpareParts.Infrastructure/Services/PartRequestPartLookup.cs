namespace SpareParts.Infrastructure.Services;

/// <summary>Minimal catalog part projection used by <see cref="PartRequestsService"/> when creating a part request.</summary>
internal sealed class PartRequestPartLookup
{
    public int Id { get; set; }
    public string InternalCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? OEMNumber { get; set; }
    public int? TenantId { get; set; }
}
