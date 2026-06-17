namespace SpareParts.Infrastructure.Services;

internal sealed class NewVsUsedRow
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal SalePrice { get; set; }
    public decimal? OemNewPrice { get; set; }
    public decimal? AftermarketNewPrice { get; set; }
    public string? Currency { get; set; }
}
