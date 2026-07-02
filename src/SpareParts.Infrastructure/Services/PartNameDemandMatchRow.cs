namespace SpareParts.Infrastructure.Services;

/// <summary>
/// Raw SQL projection row used by <see cref="DemandMatchingService.FindMatchesForParts"/> to
/// batch-match several part names against part requests / need-board ads in a single query per
/// source. <see cref="PartName"/> is the demand-side name (the requested part name or wanted-ad
/// part name) so the caller can attribute each row back to the catalog part name(s) it matches.
/// </summary>
internal sealed class PartNameDemandMatchRow
{
    public string PartName { get; set; } = string.Empty;
    public int SourceId { get; set; }
    public string SourceType { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
}
