namespace SpareParts.Domain.Negotiation;

public class NegotiationSession
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public int PartId { get; set; }
    public string BuyerName { get; set; } = string.Empty;
    public string? BuyerPhone { get; set; }
    public decimal AskingPrice { get; set; }
    public decimal? BuyerMaxPrice { get; set; }
    public decimal? SellerMinPrice { get; set; }
    public decimal? CurrentOffer { get; set; }
    public NegotiationStatus Status { get; set; }
    public string Currency { get; set; } = "USD";
    public DateTime CreatedAt { get; set; }
    public int CreatedByUserId { get; set; }
}
