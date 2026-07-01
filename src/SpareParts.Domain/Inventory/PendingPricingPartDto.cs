namespace SpareParts.Domain.Inventory
{
    /// <summary>
    /// A draft part (created by the Intake &amp; Teardown Agent) that the Pricing Agent has not
    /// yet priced — <c>PricingStatus</c> is still <c>"Manual"</c> and it is linked to a used car.
    /// </summary>
    public sealed class PendingPricingPartDto
    {
        public int PartId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int UsedCarId { get; set; }
    }
}
