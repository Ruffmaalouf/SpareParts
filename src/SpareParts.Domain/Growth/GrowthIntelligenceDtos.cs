namespace SpareParts.Domain.Growth;

public sealed class GrowthBriefingDto
{
    public DateTime GeneratedAt { get; set; }
    public string Currency { get; set; } = "USD";
    public decimal UnlockableValue { get; set; }
    public List<MoneyActionDto> MoneyActions { get; set; } = new();
    public List<DonorCarTreasureDto> DonorCars { get; set; } = new();
    public List<TeardownQueueItemDto> TeardownQueue { get; set; } = new();
    public List<DuplicatePartGroupDto> DuplicateGroups { get; set; } = new();
    public List<BuyingRadarItemDto> BuyingRadar { get; set; } = new();
}

public sealed class MoneyActionDto
{
    public string Key { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;
    public string Tone { get; set; } = "Neutral";
    public string Title { get; set; } = string.Empty;
    public string Detail { get; set; } = string.Empty;
    public decimal EstimatedValue { get; set; }
    public string Currency { get; set; } = "USD";
    public string TargetView { get; set; } = string.Empty;
    public int? PartId { get; set; }
    public int? PartRequestId { get; set; }
    public int? UsedCarId { get; set; }
}

public sealed class DonorCarTreasureDto
{
    public int UsedCarId { get; set; }
    public string Barcode { get; set; } = string.Empty;
    public string Car { get; set; } = string.Empty;
    public int ModelYear { get; set; }
    public decimal LoadedCost { get; set; }
    public decimal RecoveredValue { get; set; }
    public decimal BreakEvenGap { get; set; }
    public decimal ProjectedStockRevenue { get; set; }
    public int RecoveryPercent { get; set; }
    public List<DonorCarHotspotDto> Hotspots { get; set; } = new();
    public List<DonorCarPartOpportunityDto> Parts { get; set; } = new();
}

public sealed class DonorCarHotspotDto
{
    public string Zone { get; set; } = string.Empty;
    public int PartCount { get; set; }
    public int WaitingCustomers { get; set; }
    public decimal OpportunityValue { get; set; }
}

public sealed class DonorCarPartOpportunityDto
{
    public int PartId { get; set; }
    public string InternalCode { get; set; } = string.Empty;
    public string PartName { get; set; } = string.Empty;
    public string? OemNumber { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string Zone { get; set; } = string.Empty;
    public int AvailableQuantity { get; set; }
    public int WaitingCustomers { get; set; }
    public decimal SalePrice { get; set; }
    public decimal SuggestedPrice { get; set; }
    public decimal OpportunityValue { get; set; }
    public int PriorityScore { get; set; }
    public string Action { get; set; } = string.Empty;
}

public sealed class TeardownQueueItemDto
{
    public int UsedCarId { get; set; }
    public string Car { get; set; } = string.Empty;
    public int? PartId { get; set; }
    public string PartName { get; set; } = string.Empty;
    public string Zone { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public int PriorityScore { get; set; }
    public decimal EstimatedValue { get; set; }
}

public sealed class DuplicatePartGroupDto
{
    public string MatchKey { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public int CandidateCount { get; set; }
    public decimal CombinedStock { get; set; }
    public List<DuplicatePartCandidateDto> Candidates { get; set; } = new();
}

public sealed class DuplicatePartCandidateDto
{
    public int PartId { get; set; }
    public string InternalCode { get; set; } = string.Empty;
    public string PartName { get; set; } = string.Empty;
    public string? OemNumber { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public decimal AvailableQuantity { get; set; }
    public decimal SalePrice { get; set; }
}

public sealed class BuyingRadarItemDto
{
    public int PartId { get; set; }
    public string InternalCode { get; set; } = string.Empty;
    public string PartName { get; set; } = string.Empty;
    public string? OemNumber { get; set; }
    public string Currency { get; set; } = "USD";
    public decimal AvailableQuantity { get; set; }
    public decimal SoldQuantityLast90 { get; set; }
    public int WaitingCustomers { get; set; }
    public int MinStock { get; set; }
    public int SuggestedQuantity { get; set; }
    public decimal EstimatedRevenue { get; set; }
    public int UrgencyScore { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public sealed class AuctionProfitSimulationRequest
{
    public int? CarModelId { get; set; }
    public int? ModelYear { get; set; }
    public decimal AuctionPrice { get; set; }
    public decimal Transportation { get; set; }
    public decimal Customs { get; set; }
    public decimal Shipping { get; set; }
    public decimal Repairs { get; set; }
    public decimal PartOutCost { get; set; }
    public decimal DesiredProfit { get; set; }
}

public sealed class AuctionProfitSimulationDto
{
    public string Car { get; set; } = "Donor car";
    public decimal LoadedCost { get; set; }
    public decimal EstimatedRecovery { get; set; }
    public decimal EstimatedProfit { get; set; }
    public decimal MaximumBid { get; set; }
    public decimal HistoricalAverageRecovery { get; set; }
    public int ComparableCars { get; set; }
    public string Confidence { get; set; } = "Starter";
    public List<string> Notes { get; set; } = new();
}

public sealed class VoiceQuoteRequest
{
    public string Transcript { get; set; } = string.Empty;
    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }
}

public sealed class VoiceQuoteDto
{
    public string Transcript { get; set; } = string.Empty;
    public string SearchText { get; set; } = string.Empty;
    public string VehicleHint { get; set; } = string.Empty;
    public string DraftMessage { get; set; } = string.Empty;
    public List<VoiceQuoteMatchDto> Matches { get; set; } = new();
}

public sealed class VoiceQuoteMatchDto
{
    public int PartId { get; set; }
    public string InternalCode { get; set; } = string.Empty;
    public string PartName { get; set; } = string.Empty;
    public string? OemNumber { get; set; }
    public string Currency { get; set; } = "USD";
    public decimal SalePrice { get; set; }
    public decimal AvailableQuantity { get; set; }
    public int Score { get; set; }
}
