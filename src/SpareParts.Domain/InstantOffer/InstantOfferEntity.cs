namespace SpareParts.Domain.InstantOffer;

public class InstantOfferEntity
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public string PartName { get; set; } = string.Empty;
    public string? PartDescription { get; set; }
    public string? PhotoUrls { get; set; }
    public string? ConditionGrade { get; set; }
    public decimal EstimatedValue { get; set; }
    public decimal OfferedPrice { get; set; }
    public string Currency { get; set; } = "USD";
    public string ContactName { get; set; } = string.Empty;
    public string? ContactPhone { get; set; }
    public InstantOfferStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}
