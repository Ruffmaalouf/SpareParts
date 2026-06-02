using System.ComponentModel.DataAnnotations;

namespace SpareParts.Domain.Inventory;

public class PartSubstituteDto
{
    public int Id { get; set; }
    public int PartId { get; set; }
    public int SubstitutePartId { get; set; }
    public string SubstitutePartName { get; set; } = string.Empty;
    public string SubstitutePartCode { get; set; } = string.Empty;
    public int SubstituteAvailableStock { get; set; }
    public decimal SubstituteSalePrice { get; set; }
    public string? Notes { get; set; }
}

public class AddPartSubstituteRequest
{
    [Range(1, int.MaxValue)] public int SubstitutePartId { get; set; }
    [MaxLength(500)] public string? Notes { get; set; }
}
