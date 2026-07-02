namespace SpareParts.Infrastructure.Services;

/// <summary>Raw tenant activity/verification projection used by <see cref="SellerReputationService"/> to compute a seller's reputation score.</summary>
internal sealed class SellerDataRow
{
    public int TenantId { get; set; }
    public string TenantName { get; set; } = string.Empty;
    public int ActivePartsCount { get; set; }
    public int ReturnMovements { get; set; }
    public int TotalMovements { get; set; }
    public int? VerificationStatus { get; set; }
}
