namespace SpareParts.Infrastructure.Services;

/// <summary>A single symptom-to-part rulebook entry used by <see cref="SymptomSearchService"/>'s static diagnosis rules.</summary>
internal sealed record SymptomRule(
    string[] Keywords,
    string PartName,
    string PartNameAr,
    string Category,
    string[] SearchTerms);
