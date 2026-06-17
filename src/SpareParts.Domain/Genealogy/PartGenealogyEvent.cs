namespace SpareParts.Domain.Genealogy;

public class PartGenealogyEvent
{
    public int Id { get; set; }
    public int PartId { get; set; }
    public GenealogyEventType EventType { get; set; }
    public string? Description { get; set; }
    public string? ActorName { get; set; }
    public string? TransactionRef { get; set; }
    public DateTime? OccurredAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public int CreatedByUserId { get; set; }
}
