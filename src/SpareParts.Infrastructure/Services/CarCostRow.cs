namespace SpareParts.Infrastructure.Services;

/// <summary>
/// The subset of a used car's cost/sell-through data the Pricing Agent needs to run the
/// cost-allocation engine for a batch of draft parts.
/// </summary>
internal sealed class CarCostRow
{
    public decimal GrandTotalBase { get; set; }
    public decimal ExpectedSellThroughRate { get; set; }
    public int? CreatedByUserId { get; set; }
}
