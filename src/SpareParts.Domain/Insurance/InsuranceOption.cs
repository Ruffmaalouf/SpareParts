namespace SpareParts.Domain.Insurance;

public class InsuranceOption
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal PricePercent { get; set; }
    public decimal? MaxCoverageAmount { get; set; }
    public string Currency { get; set; } = "USD";
    public bool IsActive { get; set; }
}
