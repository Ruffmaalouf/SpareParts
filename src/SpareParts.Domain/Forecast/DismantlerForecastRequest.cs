namespace SpareParts.Domain.Forecast;

public class DismantlerForecastRequest
{
    public string VehicleMake { get; set; } = string.Empty;
    public string? VehicleModel { get; set; }
    public int? VehicleYear { get; set; }
    public string? VehicleCondition { get; set; }
    public decimal PurchasePrice { get; set; }
    public string Currency { get; set; } = "USD";
}
