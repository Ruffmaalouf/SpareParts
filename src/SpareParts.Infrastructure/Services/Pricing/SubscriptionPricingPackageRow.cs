namespace SpareParts.Infrastructure.Services.Pricing;

/// <summary>Raw <c>dbo.PricingPackages</c> row used by <see cref="SubscriptionService"/> for checkout/plan-change pricing lookups.</summary>
internal sealed class SubscriptionPricingPackageRow
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal MonthlyPrice { get; set; }
    public decimal YearlyPrice { get; set; }
    public string Currency { get; set; } = "USD";
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
    public bool IsCustomPricing { get; set; }
}
