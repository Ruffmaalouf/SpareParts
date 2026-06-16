namespace SpareParts.Domain.Marketplace
{
    public class CarCrushPartSuggestion
    {
        public string PartName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public bool IsHighValue { get; set; }
        public bool IsFastMoving { get; set; }
        public string? DefaultOemHint { get; set; }
        public string? Notes { get; set; }
    }
}
