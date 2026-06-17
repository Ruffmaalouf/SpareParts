namespace SpareParts.Domain.Reports;

public class PriceReportFilter
{
    public string? PartName { get; set; }
    public string? Make { get; set; }
    public string? Model { get; set; }
    public int? Year { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}
