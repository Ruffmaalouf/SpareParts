namespace SpareParts.Domain.Forecast;

public class DismantlerForecastResult
{
    public string VehicleDescription { get; set; } = string.Empty;
    public decimal PurchasePrice { get; set; }
    public decimal EstimatedPartOutValue { get; set; }
    public decimal EstimatedProfit { get; set; }
    public decimal EstimatedRoiPercent { get; set; }
    public string Currency { get; set; } = "USD";
    public List<PartForecastLine> TopParts { get; set; } = new();
    public string Disclaimer { get; set; } = string.Empty;
}
