namespace SpareParts.Domain.Genealogy;

public class PartGenealogyDto
{
    public int PartId { get; set; }
    public string PartName { get; set; } = string.Empty;
    public string? InternalCode { get; set; }
    public List<PartGenealogyEventDto> Events { get; set; } = new();
}
