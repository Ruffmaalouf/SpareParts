namespace SpareParts.Domain.Passport;

public class PartPassportPhoto
{
    public int Id { get; set; }
    public int PartId { get; set; }
    public int? TransactionId { get; set; }
    public PassportPhotoType PhotoType { get; set; }
    public string PhotoUrl { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public int CreatedByUserId { get; set; }
}
