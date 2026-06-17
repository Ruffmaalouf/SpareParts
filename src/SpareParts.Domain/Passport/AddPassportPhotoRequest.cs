namespace SpareParts.Domain.Passport;

public class AddPassportPhotoRequest
{
    public int PartId { get; set; }
    public int? TransactionId { get; set; }
    public PassportPhotoType PhotoType { get; set; }
    public string PhotoUrl { get; set; } = string.Empty;
    public string? Notes { get; set; }
}
