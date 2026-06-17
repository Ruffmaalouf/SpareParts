namespace SpareParts.Domain.Negotiation;

public class NegotiationOffer
{
    public int Id { get; set; }
    public int SessionId { get; set; }
    public string OfferedByRole { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }
}
