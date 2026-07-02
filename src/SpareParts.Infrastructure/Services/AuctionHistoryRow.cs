namespace SpareParts.Infrastructure.Services;

/// <summary>Aggregated donor-car auction recovery history used by <see cref="GrowthIntelligenceService"/> to simulate auction profit.</summary>
internal sealed class AuctionHistoryRow
{
    public int ComparableCars { get; set; }
    public decimal AverageRecovery { get; set; }
}
