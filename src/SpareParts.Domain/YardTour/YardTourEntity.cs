namespace SpareParts.Domain.YardTour;

public class YardTourEntity
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime? ScheduledAt { get; set; }
    public string? StreamUrl { get; set; }
    public YardTourStatus Status { get; set; }
    public int ViewerCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public int CreatedByUserId { get; set; }
}
