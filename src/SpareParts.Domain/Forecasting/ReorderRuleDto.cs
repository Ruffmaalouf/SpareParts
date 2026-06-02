namespace SpareParts.Domain.Forecasting;

public class ReorderRuleDto
{
    public int Id { get; set; }
    public int PartId { get; set; }
    public string PartName { get; set; } = string.Empty;
    public string PartCode { get; set; } = string.Empty;
    public int ReorderPoint { get; set; }    // trigger reorder when stock <= this
    public int ReorderQuantity { get; set; } // suggest ordering this many
    public int? PreferredSupplierId { get; set; }
    public string? PreferredSupplierName { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
