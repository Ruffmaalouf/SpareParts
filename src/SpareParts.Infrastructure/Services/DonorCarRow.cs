namespace SpareParts.Infrastructure.Services;

/// <summary>Raw donor-car projection used by <see cref="GrowthIntelligenceService"/> to build donor car treasure cards.</summary>
internal sealed class DonorCarRow
{
    public int UsedCarId { get; set; }
    public string Barcode { get; set; } = string.Empty;
    public string Car { get; set; } = string.Empty;
    public int ModelYear { get; set; }
    public decimal LoadedCost { get; set; }
    public decimal RecoveredValue { get; set; }
}
