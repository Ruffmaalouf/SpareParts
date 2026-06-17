namespace SpareParts.Domain.Pricing;

public class PriceGeniusRequest
{
    public int PartId { get; set; }
    public string? OverrideCurrency { get; set; }
}
