namespace SpareParts.Domain.Compatibility
{
    public class CompatibilityCheckResult
    {
        public int PartId { get; set; }
        public CompatibilityVerdict Verdict { get; set; }
        public string VerdictLabel { get; set; } = string.Empty;
        public string? Reason { get; set; }
        public List<string> CompatibleMakes { get; set; } = new();
        public List<string> CompatibleModels { get; set; } = new();
        public int? YearFrom { get; set; }
        public int? YearTo { get; set; }
        public List<string> OemNumbers { get; set; } = new();
    }
}
