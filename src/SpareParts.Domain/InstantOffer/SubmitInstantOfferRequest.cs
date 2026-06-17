namespace SpareParts.Domain.InstantOffer;

public class SubmitInstantOfferRequest
{
    public string PartName { get; set; } = string.Empty;
    public string? PartDescription { get; set; }
    public List<string> PhotoUrls { get; set; } = new();
    public string? ConditionGrade { get; set; }
    public string ContactName { get; set; } = string.Empty;
    public string? ContactPhone { get; set; }
    public string? Currency { get; set; }
}
