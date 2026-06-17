namespace SpareParts.Domain.YardTour;

public class YardTourDto
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public string? SellerName { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime? ScheduledAt { get; set; }
    public string? StreamUrl { get; set; }
    public YardTourStatus Status { get; set; }
    public string StatusLabel { get; set; } = string.Empty;
    public int ViewerCount { get; set; }
    public int InterestCount { get; set; }
    public DateTime CreatedAt { get; set; }
}
