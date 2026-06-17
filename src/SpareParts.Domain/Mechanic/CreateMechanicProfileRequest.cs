namespace SpareParts.Domain.Mechanic;

public class CreateMechanicProfileRequest
{
    public int UserId { get; set; }
    public string? SpecialtyBrands { get; set; }
    public string? SpecialtyTypes { get; set; }
}
