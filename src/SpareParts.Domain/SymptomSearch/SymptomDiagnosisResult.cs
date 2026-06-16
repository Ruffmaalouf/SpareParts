namespace SpareParts.Domain.SymptomSearch
{
    public class SymptomDiagnosisResult
    {
        public string SuggestedPartName { get; set; } = string.Empty;
        public string SuggestedPartNameAr { get; set; } = string.Empty;
        public decimal ConfidencePercent { get; set; }
        public string Reasoning { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public List<string> SearchTerms { get; set; } = new();
    }
}
