namespace SpareParts.Domain.InstantOffer;

public class UpdateInstantOfferStatusRequest
{
    public InstantOfferStatus NewStatus { get; set; }
    public string? Notes { get; set; }
}
