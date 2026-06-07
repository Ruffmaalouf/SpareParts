namespace SpareParts.Desktop.Abstractions.Parts;

public sealed class PartWorkspaceRequest
{
    public int PartId { get; init; }
    public string PartName { get; init; } = string.Empty;
}
