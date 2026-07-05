namespace SpareParts.Desktop.Wpf.Interfaces
{
    /// <summary>Typeahead row returned by GET /api/clients/search (Report 04 §06).</summary>
    public sealed class IgnitionClientSearchResultDto
    {
        public int CustomerId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public decimal Balance { get; set; }
        public string CurrencyCode { get; set; } = "USD";
    }
}
