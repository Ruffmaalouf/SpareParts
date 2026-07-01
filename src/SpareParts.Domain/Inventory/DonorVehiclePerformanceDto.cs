namespace SpareParts.Domain.Inventory
{
    /// <summary>
    /// Rolls up part sell-through and dead-stock counts by donor vehicle make/model, so the
    /// Buying Advisor Agent can recommend what to prioritize buying next.
    /// </summary>
    public sealed class DonorVehiclePerformanceDto
    {
        public string Make { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public int SoldQty90d { get; set; }
        public int DeadPartCount { get; set; }
    }
}
