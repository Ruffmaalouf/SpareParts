namespace SpareParts.Domain.Negotiation;

public class CreateNegotiationRequest
{
    public int PartId { get; set; }
    public string BuyerName { get; set; } = string.Empty;
    public string? BuyerPhone { get; set; }
    public decimal InitialOffer { get; set; }
    public decimal? BuyerMaxPrice { get; set; }
    public string? Currency { get; set; }
}
