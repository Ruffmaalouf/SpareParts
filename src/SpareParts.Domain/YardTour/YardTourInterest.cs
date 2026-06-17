namespace SpareParts.Domain.YardTour;

public class YardTourInterest
{
    public int Id { get; set; }
    public int YardTourId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? PartRequests { get; set; }
    public DateTime CreatedAt { get; set; }
}
