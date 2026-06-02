using System.ComponentModel.DataAnnotations;

namespace SpareParts.Domain.Loyalty;

public class AddLoyaltyPointsRequest
{
    [Range(1, int.MaxValue)] public int CustomerId { get; set; }
    [Range(1, 100000)] public int Points { get; set; }
    [MaxLength(50)] public string TransactionType { get; set; } = "Adjustment";
    public string? ReferenceType { get; set; }
    public int? ReferenceId { get; set; }
    [MaxLength(500)] public string? Notes { get; set; }
}

public class RedeemLoyaltyPointsRequest
{
    [Range(1, int.MaxValue)] public int CustomerId { get; set; }
    [Range(1, 100000)] public int Points { get; set; }
    [MaxLength(500)] public string? Notes { get; set; }
}
