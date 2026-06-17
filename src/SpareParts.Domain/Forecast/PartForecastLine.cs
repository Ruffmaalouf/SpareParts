namespace SpareParts.Domain.Forecast;

public class PartForecastLine
{
    public string PartName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal EstimatedPrice { get; set; }
    public bool IsHighValue { get; set; }
    public bool IsFastMoving { get; set; }
    public string ConfidenceLevel { get; set; } = "Medium";
}
