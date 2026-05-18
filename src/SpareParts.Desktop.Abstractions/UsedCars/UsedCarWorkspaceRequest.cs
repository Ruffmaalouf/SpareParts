namespace SpareParts.Desktop.Abstractions.UsedCars;

public sealed class UsedCarWorkspaceRequest
{
    public int UsedCarId { get; init; }
    public string UsedCarName { get; init; } = string.Empty;
}
