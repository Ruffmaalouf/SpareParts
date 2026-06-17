namespace SpareParts.Domain.Insurance;

public class AddInsuranceAddonRequest
{
    public int? PartId { get; set; }
    public int? SalesInvoiceId { get; set; }
    public int InsuranceOptionId { get; set; }
}
