namespace SpareParts.Domain.Search;

public sealed class SmartSearchResultDto
{
    public string Section { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string Badge { get; set; } = string.Empty;
    public string ActionLabel { get; set; } = string.Empty;
    public string TargetView { get; set; } = string.Empty;
    public string ApiRoute { get; set; } = string.Empty;
    public int Score { get; set; }
}
