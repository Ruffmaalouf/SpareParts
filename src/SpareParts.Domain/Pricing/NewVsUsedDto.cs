namespace SpareParts.Domain.Pricing;

public class NewVsUsedDto
{
    public int PartId { get; set; }
    public string PartName { get; set; } = string.Empty;
    public decimal? UsedPrice { get; set; }
    public decimal? OemNewPrice { get; set; }
    public decimal? AftermarketNewPrice { get; set; }
    public decimal? SavingsVsOem { get; set; }
    public decimal? SavingsVsAftermarket { get; set; }
    public decimal? SavingsPercentVsOem { get; set; }
    public decimal? SavingsPercentVsAftermarket { get; set; }
    public string Currency { get; set; } = "USD";
}
