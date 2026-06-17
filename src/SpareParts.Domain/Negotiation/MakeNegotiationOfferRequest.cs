namespace SpareParts.Domain.Negotiation;

public class MakeNegotiationOfferRequest
{
    public decimal Amount { get; set; }
    public string? Note { get; set; }
    public string OfferedByRole { get; set; } = "Buyer";
}
