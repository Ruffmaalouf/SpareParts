namespace SpareParts.Infrastructure.Services;

internal sealed class RegionalDemandRow
{
    public string? Region { get; set; }
    public string? City { get; set; }
    public string PartName { get; set; } = string.Empty;
    public int DemandCount { get; set; }
}
