namespace SpareParts.Domain.Mechanic;

public class MechanicProfileDto
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public int UserId { get; set; }
    public string? UserName { get; set; }
    public MechanicBadgeLevel BadgeLevel { get; set; }
    public string BadgeLevelLabel { get; set; } = string.Empty;
    public string? SpecialtyBrands { get; set; }
    public string? SpecialtyTypes { get; set; }
    public int PurchaseCount { get; set; }
    public int RepairOrderCount { get; set; }
    public bool IsVerified { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
