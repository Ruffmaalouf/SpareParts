namespace SpareParts.Domain.Passport;

public class PartPassportDto
{
    public int PartId { get; set; }
    public string PartName { get; set; } = string.Empty;
    public string? InternalCode { get; set; }
    public List<PartPassportPhotoDto> Photos { get; set; } = new();
}
