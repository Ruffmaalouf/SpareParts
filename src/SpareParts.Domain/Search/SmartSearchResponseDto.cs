namespace SpareParts.Domain.Search;

public sealed class SmartSearchResponseDto
{
    public string Query { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
    public List<SmartSearchResultDto> Results { get; set; } = new();
}
