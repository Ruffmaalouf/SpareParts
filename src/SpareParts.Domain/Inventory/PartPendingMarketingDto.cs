namespace SpareParts.Domain.Inventory
{
    /// <summary>
    /// A part the Pricing Agent has activated that the Marketing Agent has not yet scanned
    /// against open customer demand (part requests / need-board ads).
    /// </summary>
    public sealed class PartPendingMarketingDto
    {
        public int PartId { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal SalePrice { get; set; }
        public string? Currency { get; set; }

        /// <summary>The owner who logged the donor car's purchase, used to attribute the automated notification.</summary>
        public int CreatedByUserId { get; set; }
    }
}
