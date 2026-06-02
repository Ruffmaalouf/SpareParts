using System.ComponentModel.DataAnnotations;

namespace SpareParts.Domain.Forecasting;

public class UpsertReorderRuleRequest
{
    [Range(1, int.MaxValue)] public int PartId { get; set; }
    [Range(0, int.MaxValue)] public int ReorderPoint { get; set; }
    [Range(1, int.MaxValue)] public int ReorderQuantity { get; set; }
    public int? PreferredSupplierId { get; set; }
    public bool IsActive { get; set; } = true;
}
