namespace SpareParts.Domain.Mechanic;

public class MechanicProfile
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public int UserId { get; set; }
    public MechanicBadgeLevel BadgeLevel { get; set; }
    public string? SpecialtyBrands { get; set; }
    public string? SpecialtyTypes { get; set; }
    public int PurchaseCount { get; set; }
    public int RepairOrderCount { get; set; }
    public bool IsVerified { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public int CreatedByUserId { get; set; }
}
