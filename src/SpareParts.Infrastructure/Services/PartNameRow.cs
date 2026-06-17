namespace SpareParts.Infrastructure.Services;

internal sealed class PartNameRow
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? InternalCode { get; set; }
}
