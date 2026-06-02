using System.ComponentModel.DataAnnotations;

namespace SpareParts.Domain.Warranty;

public class WarrantyClaimDto
{
    public int Id { get; set; }
    public string ClaimNumber { get; set; } = string.Empty;
    public int? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public int PartId { get; set; }
    public string PartName { get; set; } = string.Empty;
    public string PartCode { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string ClaimType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Resolution { get; set; }
    public int? OriginalInvoiceId { get; set; }
    public decimal? RefundAmount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
}

public class CreateWarrantyClaimRequest
{
    public int? CustomerId { get; set; }
    [Range(1, int.MaxValue)] public int PartId { get; set; }
    [Range(1, int.MaxValue)] public int Quantity { get; set; } = 1;
    [MaxLength(50)] public string ClaimType { get; set; } = "Return";
    [MaxLength(1000)] public string? Description { get; set; }
    public int? OriginalInvoiceId { get; set; }
}

public class ResolveWarrantyClaimRequest
{
    [MaxLength(20)] public string Status { get; set; } = "Resolved";
    [MaxLength(1000)] public string? Resolution { get; set; }
    public decimal? RefundAmount { get; set; }
}
