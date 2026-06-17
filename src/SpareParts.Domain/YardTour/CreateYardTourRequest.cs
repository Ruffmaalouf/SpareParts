namespace SpareParts.Domain.YardTour;

public class CreateYardTourRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime? ScheduledAt { get; set; }
    public string? StreamUrl { get; set; }
}
