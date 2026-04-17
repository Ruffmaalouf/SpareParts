namespace SpareParts.Domain.Inventory
{
    public sealed class GeneratePartNotesResponse
    {
        public string SuggestedNotes { get; set; } = string.Empty;
        public decimal? SuggestedAveragePrice { get; set; }
    }
}
