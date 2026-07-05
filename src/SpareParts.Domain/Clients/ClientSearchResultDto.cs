namespace SpareParts.Domain.Clients
{
    /// <summary>One typeahead hit for GET /api/clients/search (Report 04 §06).</summary>
    public sealed class ClientSearchResultDto
    {
        public int CustomerId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public decimal Balance { get; set; }
        public string CurrencyCode { get; set; } = "USD";
    }
}
