namespace SpareParts.Domain.YardTour;

public class SubmitYardTourInterestRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? PartRequests { get; set; }
}
