namespace SpareParts.Infrastructure.Services;

internal sealed class NegotiationBotRow
{
    public int Id { get; set; }
    public decimal AskingPrice { get; set; }
    public decimal? BuyerMaxPrice { get; set; }
    public decimal? SellerMinPrice { get; set; }
    public int Status { get; set; }
}
