namespace SpareParts.Desktop.Wpf.Management
{
    public sealed class ManagementOperationResult
    {
        public bool Success { get; init; }
        public required string Message { get; init; }
    }
}
