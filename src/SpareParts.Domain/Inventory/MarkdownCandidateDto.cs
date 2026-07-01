namespace SpareParts.Domain.Inventory
{
    /// <summary>
    /// A currently active, unsold-for-a-while part the Dead Stock Markdown Agent is evaluating.
    /// </summary>
    public sealed class MarkdownCandidateDto
    {
        public int PartId { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal SalePrice { get; set; }
        public decimal FastSalePrice { get; set; }
        public decimal MinimumSellPrice { get; set; }
        public DateTime? PricingCalculatedAt { get; set; }
    }
}
