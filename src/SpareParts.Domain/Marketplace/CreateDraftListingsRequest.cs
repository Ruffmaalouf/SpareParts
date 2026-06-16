namespace SpareParts.Domain.Marketplace;

public class CreateDraftListingsRequest
{
    public CarCrushRequest? Vehicle { get; set; }
    public List<string> SelectedPartNames { get; set; } = new();
}
