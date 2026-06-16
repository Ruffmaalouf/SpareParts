namespace SpareParts.Domain.SymptomSearch
{
    public class SymptomSearchResponse
    {
        public List<SymptomDiagnosisResult> Results { get; set; } = new();
        public string DiagnosticSummary { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
    }
}
