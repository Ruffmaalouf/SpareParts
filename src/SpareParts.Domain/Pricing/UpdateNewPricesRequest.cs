namespace SpareParts.Domain.Pricing;

public class UpdateNewPricesRequest
{
    public int PartId { get; set; }
    public decimal? OemNewPrice { get; set; }
    public decimal? AftermarketNewPrice { get; set; }
}
