namespace SpareParts.Domain.Marketplace
{
    public class CarCrushResult
    {
        public string VehicleDescription { get; set; } = string.Empty;
        public List<CarCrushPartSuggestion> Suggestions { get; set; } = new();
        public int TotalSuggestions { get; set; }
        public string? Disclaimer { get; set; }
    }
}
