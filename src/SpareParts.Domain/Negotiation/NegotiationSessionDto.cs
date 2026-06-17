namespace SpareParts.Domain.Negotiation;

public class NegotiationSessionDto
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public int PartId { get; set; }
    public string? PartName { get; set; }
    public string BuyerName { get; set; } = string.Empty;
    public string? BuyerPhone { get; set; }
    public decimal AskingPrice { get; set; }
    public decimal? CurrentOffer { get; set; }
    public NegotiationStatus Status { get; set; }
    public string StatusLabel { get; set; } = string.Empty;
    public string Currency { get; set; } = "USD";
    public List<NegotiationOfferDto> Offers { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}
