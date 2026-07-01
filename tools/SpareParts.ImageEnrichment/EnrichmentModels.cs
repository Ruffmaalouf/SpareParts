namespace SpareParts.ImageEnrichment;

internal sealed record ImageCandidateAudit
{
    public int PartId { get; init; }
    public string InternalCode { get; init; } = "";
    public string PartName { get; init; } = "";
    public string? Vehicle { get; init; }
    public int CandidateRank { get; set; }
    public string SearchQueryUsed { get; init; } = "";
    public string QueryTier { get; init; } = "";
    public string ImageUrl { get; init; } = "";
    public string? ThumbUrl { get; init; }
    public string? SourcePageUrl { get; init; }
    public string? SourceDomain { get; init; }
    public string? Provider { get; init; }
    public string? License { get; init; }
    public bool LicenseKnown { get; init; }
    public double ConfidenceScore { get; init; }
    public string ConfidenceLevel { get; init; } = "low";
    public string? MatchReason { get; init; }
    public bool ImageReachable { get; init; }
    public string? ContentType { get; init; }
    public long? ContentLengthBytes { get; init; }
    public string? RejectionReason { get; init; }
    public string Status { get; init; } = "rejected";
}

internal sealed record OemEnrichmentResult
{
    public int PartId { get; init; }
    public string InternalCode { get; init; } = "";
    public string PartName { get; init; } = "";
    public string? Vehicle { get; init; }
    public string? ExistingOemNumber { get; init; }
    public string? ProposedPrimaryOemNumber { get; set; }
    public string? AlternativeOemNumbers { get; set; }
    public string? ManufacturerPartNumber { get; set; }
    public string? SupplierPartNumber { get; set; }
    public string? SourceUrl { get; set; }
    public string? SourceDomain { get; set; }
    public string? SearchQueryUsed { get; set; }
    public int SearchQueriesExecuted { get; set; }
    public int TextResultsFound { get; set; }
    public double ConfidenceScore { get; set; }
    public string ConfidenceLevel { get; set; } = "low";
    public string? MatchReason { get; set; }
    public string Status { get; set; } = "skipped";
    public string? ErrorMessage { get; set; }
}

internal sealed record MissingPartCandidate
{
    public int DonorVehicleId { get; init; }
    public string? Vehicle { get; init; }
    public string ExpectedPartName { get; init; } = "";
    public string? Category { get; init; }
    public string? ExpectedOemNumbers { get; init; }
    public string? CompatibleYearRange { get; init; }
    public string? Engine { get; init; }
    public string? BodyType { get; init; }
    public string? Drivetrain { get; init; }
    public string? SourceUrl { get; init; }
    public double ConfidenceScore { get; init; }
    public string Status { get; init; } = "NeedsVerification";
    public string? Notes { get; init; }
}

internal sealed record TextSearchResult(
    string Title,
    string Url,
    string Snippet,
    string Provider);

