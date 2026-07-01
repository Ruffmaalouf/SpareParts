namespace SpareParts.Desktop.Wpf.Pricing;

public sealed class SmartPricingCoachResult
{
    public string Tone { get; init; } = "neutral";
    public string Badge { get; init; } = "Price guide";
    public string Message { get; init; } = "Choose a part to see pricing guidance.";
    public decimal SuggestedPrice { get; init; }
}
