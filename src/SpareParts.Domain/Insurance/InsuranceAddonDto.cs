namespace SpareParts.Domain.Insurance;

public class InsuranceAddonDto
{
    public int Id { get; set; }
    public int TenantId { get; set; }
    public int? SalesInvoiceId { get; set; }
    public int? PartId { get; set; }
    public int InsuranceOptionId { get; set; }
    public string? OptionName { get; set; }
    public decimal Price { get; set; }
    public string Currency { get; set; } = "USD";
    public DateTime CreatedAt { get; set; }
}
