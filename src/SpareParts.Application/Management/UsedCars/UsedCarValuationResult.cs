namespace SpareParts.Application.Management.UsedCars;

public sealed class UsedCarValuationResult
{
    public decimal PriceBase { get; init; }
    public decimal PriceCounter { get; init; }
    public decimal TotalBeforeShipping { get; init; }
    public decimal GrandTotalBase { get; init; }
    public decimal GrandTotalCounter { get; init; }
}
