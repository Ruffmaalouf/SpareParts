namespace SpareParts.Application.Management.UsedCars;

public sealed class UsedCarValuationInput
{
    public decimal Price { get; init; }
    public string? PriceCurrencyCode { get; init; }
    public decimal Transportation { get; init; }
    public decimal PartOut { get; init; }
    public decimal Shipping { get; init; }
    public decimal Customs { get; init; }
    public decimal Repairs { get; init; }
}
