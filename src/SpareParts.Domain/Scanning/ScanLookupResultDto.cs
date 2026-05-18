namespace SpareParts.Domain.Scanning
{
    public sealed class ScanLookupResultDto
    {
        public string TargetType { get; set; } = string.Empty;
        public int TargetId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string DisplayText { get; set; } = string.Empty;
        public string? SecondaryText { get; set; }
        public string? ApiRoute { get; set; }
    }
}
